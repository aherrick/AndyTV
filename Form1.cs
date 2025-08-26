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
    private readonly MediaPlayer _mediaPlayer;
    private readonly NotificationService _notificationService;
    private readonly UpdateService _updateService;

    private readonly ContextMenuStrip _contextMenuStrip = new();

    private MenuRecentChannelHelper _menuRecentChannelHelper;
    private MenuSettingsHelper _menuSettingsHelper;
    private MenuTVChannelHelper _menuTVChannelHelper;
    private MenuFavoriteChannelHelper _menuFavoriteChannelHelper;

    private Channel _currentChannel = null;
    private Rectangle _manuallyAdjustedBounds = Rectangle.Empty;

    private DateTime _mouseDownLeftPrevChannel = DateTime.MinValue;
    private DateTime _mouseDownRightExit = DateTime.MinValue;

    private readonly VideoView _videoView;

    public Form1(LibVLC libVLC, MediaPlayer mediaPlayer, UpdateService updateService)
    {
        _libVLC = libVLC;
        _mediaPlayer = mediaPlayer;
        _updateService = updateService;

        InitializeComponent();

        _notificationService = new NotificationService(this);

        HandleCreated += delegate
        {
            var last = ChannelDataService.LoadLastChannel();
            if (last != null)
            {
                Play(last);
            }
        };

        Logger.Info("Starting AndyTV...");

        Icon = new Icon("AndyTV.ico");

        _mediaPlayer.Stopped += delegate
        {
            Play(_currentChannel);
        };
        _mediaPlayer.EndReached += delegate
        {
            Play(_currentChannel);
        };
        _mediaPlayer.EncounteredError += delegate
        {
            Play(_currentChannel);
        };
        _mediaPlayer.Playing += delegate
        {
            // Reset cursor based on fullscreen state
            if (FormBorderStyle == FormBorderStyle.None)
                _videoView.HideCursor();
            else
                _videoView.ShowDefault();

            _notificationService.ShowToast(_currentChannel.DisplayName);
            RecentChannelsService.AddOrPromote(_currentChannel);
            ChannelDataService.SaveLastChannel(_currentChannel);
            _menuRecentChannelHelper?.RebuildRecentMenu();
        };

        _videoView = new VideoView
        {
            Dock = DockStyle.Fill,
            MediaPlayer = _mediaPlayer,
            ContextMenuStrip = _contextMenuStrip,
        };
        _videoView.MouseDoubleClick += VideoView_MouseDoubleClick;
        _videoView.MouseUp += VideoView_MouseUp;
        _videoView.MouseDown += VideoView_MouseDown;
        Controls.Add(_videoView);

        MaximizeWindow(); // start fullscreen

        ResizeEnd += delegate
        {
            if (WindowState == FormWindowState.Normal)
                _manuallyAdjustedBounds = Bounds;
        };

        Shown += async delegate
        {
            var appVersionName = "AndyTV v" + AppHelper.Version;
            Text = appVersionName;

            _menuTVChannelHelper = new MenuTVChannelHelper(_contextMenuStrip);

            _menuSettingsHelper = new MenuSettingsHelper(
                _contextMenuStrip,
                appVersionName,
                _mediaPlayer,
                _updateService,
                () => new FavoriteChannelForm(_menuTVChannelHelper.Channels),
                () => _menuFavoriteChannelHelper.RebuildFavoritesMenu(),
                _videoView
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
                _videoView.HideCursor();
            else
                _videoView.ShowDefault();

            Logger.Info("[CHANNELS] Loaded");
        };

        _contextMenuStrip.Opening += delegate
        {
            _videoView.ShowDefault();
        };
        _contextMenuStrip.Closing += delegate
        {
            if (FormBorderStyle == FormBorderStyle.None)
                _videoView.HideCursor();
            else
                _videoView.ShowDefault();
        };
    }

    private void ChItem_Click(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item && item.Tag is Channel ch)
            Play(ch);
    }

    private void Play(Channel channel)
    {
        if (channel == null)
            return;

        Logger.Info($"[PLAY][BEGIN] channel='{channel.DisplayName}' url='{channel.Url}'");
        _currentChannel = channel;
        _videoView.ShowWaiting();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                using var media = new Media(_libVLC, channel.Url, FromType.FromLocation);
                _mediaPlayer.Play(media);
            }
            catch (Exception ex)
            {
                _videoView.ShowDefault();
                Logger.Error(
                    $"[PLAY][ERROR] channel='{channel.DisplayName}' url='{channel.Url}' ex={ex}"
                );
            }
        });
    }

    private void MaximizeWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        Bounds = Screen.FromControl(this).Bounds;

        // Hide cursor when hovering over video view
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

        _videoView.ShowDefault(); // show
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
                Play(prevChannel);
                _currentChannel = prevChannel;
            }
        }

        if (e.Button == MouseButtons.Middle)
            _mediaPlayer.Mute = !_mediaPlayer.Mute;

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