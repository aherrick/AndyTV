using AndyTV.Helpers;
using AndyTV.Helpers.Menu;
using AndyTV.Models;
using AndyTV.Services;
using AndyTV.UI;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace AndyTV;

public partial class Form1 : Form
{
    private readonly LibVLC _libVLC;
    private readonly NotificationService _notificationService;
    private readonly UpdateService _updateService;
    private readonly VideoView _videoView;

    private readonly ContextMenuStrip _contextMenuStrip = new();

    private MenuRecentChannelHelper _menuRecentChannelHelper;
    private MenuSettingsHelper _menuSettingsHelper;
    private MenuTVChannelHelper _menuTVChannelHelper;
    private MenuFavoriteChannelHelper _menuFavoriteChannelHelper;

    private Channel _currentChannel = null;
    private Rectangle _manuallyAdjustedBounds = Rectangle.Empty;

    private DateTime _mouseDownLeftPrevChannel = DateTime.MinValue;
    private DateTime _mouseDownRightExit = DateTime.MinValue;

    // 🔒 Prevent overlapping Play() attempts
    private volatile bool _isPlaying = false;

    // ⛔ Kill stale tasks/events from a previous play
    private CancellationTokenSource _playCts;

    public Form1(LibVLC libVLC, UpdateService updateService, VideoView videoView)
    {
        _libVLC = libVLC;
        _updateService = updateService;
        _videoView = videoView;

        InitializeComponent();

        _notificationService = new NotificationService(this);
        _playCts = new CancellationTokenSource();

        HandleCreated += delegate
        {
            var last = ChannelDataService.LoadLastChannel();
            if (last != null)
            {
                Play(last); // guarded; will be ignored if something is already starting
            }
        };

        Logger.Info("Starting AndyTV...");

        Icon = new Icon("AndyTV.ico");

        void RestartChannel(string trigger)
        {
            var ct = _playCts.Token; // snapshot current token
            Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(500, ct); // tiny backoff to let VLC settle
                        if (ct.IsCancellationRequested)
                        {
                            return;
                        }
                        Logger.Info(
                            $"[RESTART] trigger={trigger} ch='{_currentChannel.DisplayName}'"
                        );
                        Play(_currentChannel); // normal (non-forced) restart; guard applies
                    }
                    catch (OperationCanceledException)
                    {
                        // stale restart; ignore
                    }
                },
                ct
            );
        }

        // VLC events
        videoView.MediaPlayer.Playing += delegate
        {
            if (_playCts.IsCancellationRequested)
            {
                return; // stale event from old play
            }

            _isPlaying = false; // now safe to allow another Play()

            // Reset cursor based on fullscreen state
            if (FormBorderStyle == FormBorderStyle.None)
            {
                _videoView.HideCursor();
            }
            else
            {
                _videoView.ShowDefault();
            }

            _notificationService.ShowToast(_currentChannel.DisplayName);
            RecentChannelsService.AddOrPromote(_currentChannel);
            ChannelDataService.SaveLastChannel(_currentChannel);
            _menuRecentChannelHelper?.RebuildRecentMenu();
        };

        videoView.MediaPlayer.EndReached += (_, _) =>
        {
            if (_playCts.IsCancellationRequested)
            {
                return; // stale
            }

            _isPlaying = false;
            RestartChannel("EndReached");
        };

        videoView.MediaPlayer.EncounteredError += (_, _) =>
        {
            if (_playCts.IsCancellationRequested)
            {
                return; // stale
            }

            _isPlaying = false;
            RestartChannel("EncounteredError");
        };

        // Configure VideoView with context menu and event handlers
        _videoView.ContextMenuStrip = _contextMenuStrip;
        _videoView.MouseDoubleClick += VideoView_MouseDoubleClick;
        _videoView.MouseUp += VideoView_MouseUp;
        _videoView.MouseDown += VideoView_MouseDown;
        Controls.Add(_videoView);

        MaximizeWindow(); // start fullscreen

        ResizeEnd += delegate
        {
            if (WindowState == FormWindowState.Normal)
            {
                _manuallyAdjustedBounds = Bounds;
            }
        };

        Shown += async delegate
        {
            var appVersionName = "AndyTV v" + AppHelper.Version;
            Text = appVersionName;

            _menuTVChannelHelper = new MenuTVChannelHelper(_contextMenuStrip);

            _menuSettingsHelper = new MenuSettingsHelper(
                menu: _contextMenuStrip,
                appVersionName: appVersionName,
                updateService: _updateService,
                createFavoritesForm: () => new FavoriteChannelForm(_menuTVChannelHelper.Channels),
                rebuildFavoritesMenu: () => _menuFavoriteChannelHelper.RebuildFavoritesMenu(),
                videoView: _videoView,
                libVLC: _libVLC
            );

            _menuRecentChannelHelper = new MenuRecentChannelHelper(_contextMenuStrip, ChItem_Click);
            _menuRecentChannelHelper.RebuildRecentMenu();

            _menuFavoriteChannelHelper = new MenuFavoriteChannelHelper(
                _contextMenuStrip,
                ChItem_Click
            );
            _menuFavoriteChannelHelper.RebuildFavoritesMenu();

            var source = M3UService.TryGetFirstSource();
            if (source == null)
            {
                RestoreWindow();
                source = M3UService.PromptNewSource();
                if (source == null)
                {
                    Logger.Warn("[APP] No M3U source selected. Exiting.");
                    Close();
                    return;
                }
            }

            Logger.Info("[CHANNELS] Loading from M3U...");
            _videoView.ShowWaiting();

            await _menuTVChannelHelper.LoadChannels(ChItem_Click, source.Url);

            if (FormBorderStyle == FormBorderStyle.None)
            {
                _videoView.HideCursor();
            }
            else
            {
                _videoView.ShowDefault();
            }

            Logger.Info("[CHANNELS] Loaded");
        };

        _contextMenuStrip.Opening += delegate
        {
            _videoView.ShowDefault();
        };
        _contextMenuStrip.Closing += delegate
        {
            if (FormBorderStyle == FormBorderStyle.None)
            {
                _videoView.HideCursor();
            }
            else
            {
                _videoView.ShowDefault();
            }
        };
    }

    private void ChItem_Click(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item && item.Tag is Channel ch)
        {
            Play(ch, force: true); // manual selection overrides guard
        }
    }

    private void Play(Channel channel, bool force = false)
    {
        if (_isPlaying && !force)
        {
            return; // throttle auto restarts
        }

        _isPlaying = true;

        // Cancel any pending work from previous play (restarts, stale event reactions)
        _playCts.Cancel();
        _playCts.Dispose();
        _playCts = new CancellationTokenSource();

        Logger.Info($"[PLAY][BEGIN] channel='{channel.DisplayName}' url='{channel.Url}'");
        _currentChannel = channel;
        _videoView.ShowWaiting();

        _videoView.MediaPlayer.Stop();

        using var media = new Media(_libVLC, new Uri(channel.Url));
        _videoView.MediaPlayer.Play(media);
    }

    private void MaximizeWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        Bounds = Screen.FromControl(this).Bounds;
        _videoView.HideCursor();
    }

    private void RestoreWindow()
    {
        FormBorderStyle = FormBorderStyle.Sizable;
        WindowState = FormWindowState.Normal;

        Bounds =
            _manuallyAdjustedBounds != Rectangle.Empty
                ? _manuallyAdjustedBounds
                : Screen.FromControl(this).WorkingArea;

        _videoView.ShowDefault();
    }

    private void VideoView_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            _mouseDownLeftPrevChannel = DateTime.Now;
        if (e.Button == MouseButtons.Right)
            _mouseDownRightExit = DateTime.Now;
    }

    private void VideoView_MouseUp(object sender, MouseEventArgs e)
    {
        if (
            e.Button == MouseButtons.Left
            && _mouseDownLeftPrevChannel != DateTime.MinValue
            && _mouseDownLeftPrevChannel.AddSeconds(1) < DateTime.Now
        )
        {
            var prevChannel = RecentChannelsService.GetPrevious();
            if (prevChannel != null)
            {
                Play(prevChannel, force: true); // make previous-channel switch authoritative
                _currentChannel = prevChannel;
            }
        }

        if (e.Button == MouseButtons.Middle)
        {
            _videoView.MediaPlayer.Mute = !_videoView.MediaPlayer.Mute;
        }

        if (
            e.Button == MouseButtons.Right
            && _mouseDownRightExit != DateTime.MinValue
            && _mouseDownRightExit.AddSeconds(5) < DateTime.Now
        )
        {
            Close();
        }
    }

    private void VideoView_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        if (FormBorderStyle == FormBorderStyle.None)
            RestoreWindow();
        else
            MaximizeWindow();
    }
}