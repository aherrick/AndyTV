using System.Diagnostics;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using AndyTV.Helpers;
using AndyTV.Menu;
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
    private readonly IPlaylistService _playlistService;
    private readonly IRecentChannelService _recentChannelService;
    private readonly ILastChannelService _lastChannelService;
    private readonly IFavoriteChannelService _favoriteChannelService;

    private readonly ContextMenuStrip _contextMenuStrip = new();

    private MenuRecent _menuRecent;
    private MenuFavorite _menuFavorite;
    private readonly MenuTop _menuTop;

    private Channel _currentChannel = null;
    private Rectangle _manuallyAdjustedBounds = Rectangle.Empty;

    private DateTime _mouseDownLeftPrevChannel = DateTime.MinValue;
    private DateTime _mouseDownRightExit = DateTime.MinValue;

    private readonly StreamHealthMonitor _healthMonitor;

    private const int MOUSE_LEFT_DOUBLE_CLICK_SECONDS = 1;
    private const int MOUSE_RIGHT_EXIT_SECONDS = 5;
    private const int HOURLY_REFRESH_MILLISECONDS = 60 * 60 * 1000;
    private const int HEALTH_CHECK_MILLISECONDS = 1000;
    private System.Windows.Forms.Timer _healthTimer;

    private bool _favoritesShown = true;

    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    private System.Windows.Forms.Timer _hourlyRefreshTimer;

    public Form1(
        LibVLC libVLC,
        UpdateService updateService,
        VideoView videoView,
        IPlaylistService playlistService,
        IRecentChannelService recentChannelService,
        ILastChannelService lastChannelService,
        IFavoriteChannelService favoriteChannelService
    )
    {
        _libVLC = libVLC;
        _updateService = updateService;
        _videoView = videoView;
        _playlistService = playlistService;
        _recentChannelService = recentChannelService;
        _lastChannelService = lastChannelService;
        _favoriteChannelService = favoriteChannelService;

        _healthMonitor = new StreamHealthMonitor(
            isPaused: () => _videoView.MediaPlayer.State == VLCState.Paused,
            restart: () =>
            {
                if (_currentChannel == null)
                    return;

                Logger.Info(
                    $"[HEALTH] Restarting channel: '{_currentChannel.DisplayName}' url='{_currentChannel.Url}'"
                );
                Play(_currentChannel);
            }
        );

        InitializeComponent();

        _menuTop = new MenuTop(_contextMenuStrip, _ui, _playlistService);

        _notificationService = new NotificationService(this);

        HandleCreated += delegate
        {
            var last = _lastChannelService.LoadLastChannel();
            if (last != null)
                Play(last);
        };

        Logger.Info("Starting AndyTV...");
        Icon = new Icon("AndyTV.ico");

        _videoView.MediaPlayer.TimeChanged += delegate { _healthMonitor.MarkActivity(); };
        _videoView.MediaPlayer.PositionChanged += delegate { _healthMonitor.MarkActivity(); };

        _videoView.MediaPlayer.Playing += delegate
        {
            _healthMonitor.MarkPlaying();

            _videoView.SetCursorForCurrentView();

            _notificationService.ShowToast(_currentChannel.DisplayName);
            _recentChannelService.AddOrPromote(_currentChannel);
            _lastChannelService.SaveLastChannel(_currentChannel);
            _menuRecent?.Rebuild();
        };

        _videoView.ContextMenuStrip = _contextMenuStrip;

        _videoView.MouseDoubleClick += (_, __) =>
        {
            if (FormBorderStyle == FormBorderStyle.None)
                RestoreWindow();
            else
                MaximizeWindow();
        };

        _videoView.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                _mouseDownLeftPrevChannel = DateTime.Now;
            if (e.Button == MouseButtons.Right)
                _mouseDownRightExit = DateTime.Now;
        };

        _videoView.MouseUp += (_, e) =>
        {
            if (
                e.Button == MouseButtons.Left
                && _mouseDownLeftPrevChannel != DateTime.MinValue
                && _mouseDownLeftPrevChannel.AddSeconds(MOUSE_LEFT_DOUBLE_CLICK_SECONDS)
                    < DateTime.Now
            )
            {
                var prevChannel = _recentChannelService.GetPrevious();
                if (prevChannel != null)
                    Play(prevChannel);
            }

            if (e.Button == MouseButtons.Middle)
            {
                _videoView.MediaPlayer.Mute = !_videoView.MediaPlayer.Mute;
            }

            if (
                e.Button == MouseButtons.Right
                && _mouseDownRightExit != DateTime.MinValue
                && _mouseDownRightExit.AddSeconds(MOUSE_RIGHT_EXIT_SECONDS) < DateTime.Now
            )
            {
                Close();
            }
        };

        _videoView.MouseWheel += (_, e) =>
        {
            var recents = _recentChannelService.GetRecentChannels();
            if (recents.Count == 0)
                return;

            var currentIndex =
                _currentChannel != null
                    ? recents.FindIndex(c =>
                        string.Equals(
                            c.Url,
                            _currentChannel.Url,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    : -1;

            int nextIndex;
            if (e.Delta > 0) // Scroll up - go to previous (older)
            {
                nextIndex = currentIndex + 1;
            }
            else // Scroll down - go to next (newer)
            {
                nextIndex = currentIndex - 1;
            }

            // Wrap around
            if (nextIndex < 0)
            {
                nextIndex = recents.Count - 1;
            }
            else if (nextIndex >= recents.Count)
            {
                nextIndex = 0;
            }

            var nextChannel = recents[nextIndex];
            if (nextChannel != null)
                Play(nextChannel);
        };

        Controls.Add(_videoView);

        ResizeEnd += delegate
        {
            if (WindowState == FormWindowState.Normal)
                _manuallyAdjustedBounds = Bounds;
        };

        Shown += async delegate
        {
            Logger.Info($"[STARTUP] StartOnRight={Program.StartOnRight}");

            if (Program.StartOnRight)
            {
                SnapToHalf(left: false);
            }
            else
            {
                MaximizeWindow();
            }

            var appVersionName = "AndyTV v" + AppHelper.Version;
            Text = appVersionName;

            BuildSettingsMenu(appVersionName);

            _menuRecent = new MenuRecent(
                _contextMenuStrip,
                ChItem_Click,
                _ui,
                _recentChannelService
            );
            _menuFavorite = new MenuFavorite(
                _contextMenuStrip,
                ChItem_Click,
                _ui,
                _favoriteChannelService
            );
            _menuFavorite.Rebuild();

            // Initial refresh
            StartChannelRefresh();

            if (_playlistService.LoadPlaylists().Count == 0)
            {
                RestoreWindow();
                await HandlePlaylistManager();
            }

            _videoView.SetCursorForCurrentView();

            _healthTimer = new System.Windows.Forms.Timer { Interval = HEALTH_CHECK_MILLISECONDS };
            _healthTimer.Tick += (_, __) =>
            {
                if (_currentChannel == null)
                    return;

                _healthMonitor.Tick();
            };
            _healthTimer.Start();

            _hourlyRefreshTimer = new System.Windows.Forms.Timer
            {
                Interval = HOURLY_REFRESH_MILLISECONDS,
            };
            _hourlyRefreshTimer.Tick += (_, __) => StartChannelRefresh();
            _hourlyRefreshTimer.Start();
        };
    }

    private int _isRefreshingChannels;

    private void StartChannelRefresh()
    {
        if (Interlocked.CompareExchange(ref _isRefreshingChannels, 1, 0) != 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await _playlistService.RefreshChannelsAsync();
                _ui.Post(_ => _menuTop.Rebuild(ChItem_Click), null);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to refresh channels");
            }
            finally
            {
                Interlocked.Exchange(ref _isRefreshingChannels, 0);
            }
        });
    }

    private async Task HandlePlaylistManager()
    {
        _videoView.ShowDefault();
        using (var dlg = new PlaylistManagerForm(_playlistService))
        {
            dlg.ShowDialog(this);
            if (dlg.Saved)
            {
                StartChannelRefresh();
            }
        }
        _videoView.SetCursorForCurrentView();
    }

    private void ChItem_Click(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item && item.Tag is Channel ch)
            Play(ch);
    }

    private void Play(Channel channel)
    {
        _currentChannel = channel;

        _videoView.ShowWaiting();
        _healthMonitor.MarkPlaying();

        Logger.Info($"[PLAY][BEGIN] channel='{channel.DisplayName}' url='{channel.Url}'");

        ThreadPool.QueueUserWorkItem(_ =>
        {
            _videoView.MediaPlayer.Stop();
            using var media = new Media(_libVLC, new Uri(channel.Url));
            _videoView.MediaPlayer.Play(media);
        });
    }

    private void MaximizeWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        Bounds = Screen.PrimaryScreen.Bounds;
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

    private void SnapToHalf(bool left)
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Normal;
        var screen = Screen.PrimaryScreen.Bounds;
        var x = left ? screen.X : (screen.X + (screen.Width / 2));
        Bounds = new Rectangle(x, screen.Y, screen.Width / 2, screen.Height);
        _videoView.HideCursor();
    }

    private void BuildSettingsMenu(string appVersionName)
    {
        var header = MenuHelper.AddHeader(_contextMenuStrip, appVersionName).Header;
        header.Click += (_, __) =>
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "https://github.com/aherrick/andytv",
                    UseShellExecute = true,
                }
            );
        };

        var channelsMenu = new ToolStripMenuItem("Channels");

        MenuHelper.AddMenuItem(
            channelsMenu,
            "Guide",
            (_, __) =>
            {
                _videoView.ShowDefault();
                using var guideForm = new UI.GuideForm();
                guideForm.ShowDialog(this);
                _videoView.SetCursorForCurrentView();
            }
        );

        MenuHelper.AddMenuItem(
            channelsMenu,
            "Ad Hoc",
            (_, __) =>
            {
                _videoView.ShowDefault();
                using var dialog = new AdHocChannelForm(_playlistService.Channels);
                dialog.ShowDialog(this);
                if (dialog.SelectedItem != null)
                    Play(dialog.SelectedItem);
                _videoView.SetCursorForCurrentView();
            }
        );

        MenuHelper.AddSeparator(channelsMenu);

        MenuHelper.AddMenuItem(
            channelsMenu,
            "Manage",
            async (_, __) => await HandlePlaylistManager()
        );

        MenuHelper.AddMenuItem(channelsMenu, "Refresh", (_, __) => StartChannelRefresh());

        _contextMenuStrip.Items.Add(channelsMenu);

        var favoritesMenu = new ToolStripMenuItem("Favorites");

        void OpenFavorites(Channel addOnOpen = null)
        {
            _videoView.ShowDefault();
            using var form = new FavoriteChannelForm(
                _playlistService.Channels,
                _favoriteChannelService,
                addOnOpen
            );
            form.FormClosed += (_, __) =>
            {
                if (form.Saved)
                    _menuFavorite.Rebuild();
                _videoView.SetCursorForCurrentView();
            };
            form.ShowDialog(this);
        }

        MenuHelper.AddMenuItem(favoritesMenu, "Manage", (_, __) => OpenFavorites());

        MenuHelper.AddMenuItem(
            favoritesMenu,
            "Add Playing",
            (_, __) =>
            {
                if (_currentChannel is not null && !_menuFavorite.IsDuplicate(_currentChannel))
                {
                    OpenFavorites(_currentChannel);
                }
            }
        );

        MenuHelper.AddSeparator(favoritesMenu);

        var favoritesToggleItem = MenuHelper.AddMenuItem(
            favoritesMenu,
            "Hide",
            (_, __) =>
            {
                _favoritesShown = !_favoritesShown;
                _menuFavorite.Rebuild(show: _favoritesShown);
            }
        );

        _contextMenuStrip.Items.Add(favoritesMenu);

        var appMenu = new ToolStripMenuItem("App");

        MenuHelper.AddMenuItem(
            appMenu,
            "Update",
            async (_, __) => await _updateService.CheckForUpdates()
        );

        MenuHelper.AddMenuItem(
            appMenu,
            "Logs",
            (_, __) =>
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = Logger.LogFolder,
                        UseShellExecute = true,
                    }
                );
            }
        );

        var muteItem = MenuHelper.AddMenuItem(
            appMenu,
            "Mute",
            (_, __) => _videoView.MediaPlayer.Mute = !_videoView.MediaPlayer.Mute
        );

        var pauseItem = MenuHelper.AddMenuItem(
            appMenu,
            "Pause",
            (_, __) => _videoView.MediaPlayer.Pause()
        );

        MenuHelper.AddSeparator(appMenu);

        MenuHelper.AddMenuItem(
            appMenu,
            "New Window",
            (_, __) =>
            {
                // Only snap left if currently fullscreen
                if (Bounds == Screen.PrimaryScreen.Bounds)
                {
                    SnapToHalf(left: true);
                }

                var exePath = Application.ExecutablePath;
                Logger.Info($"[NEW WINDOW] Launching: {exePath} --new-instance --right");

                var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = "--new-instance --right",
                        UseShellExecute = true,
                        WorkingDirectory = AppContext.BaseDirectory,
                    }
                );

                Logger.Info($"[NEW WINDOW] Process started: {process?.Id}");
            }
        );

        MenuHelper.AddMenuItem(appMenu, "Restart", (_, __) => Program.Restart());
        MenuHelper.AddMenuItem(appMenu, "Exit", (_, __) => Application.Exit());

        _contextMenuStrip.Items.Add(appMenu);

        _contextMenuStrip.Opening += (_, __) =>
        {
            _videoView.ShowDefault();
            muteItem.Text = _videoView.MediaPlayer.Mute ? "Unmute" : "Mute";
            pauseItem.Text = _videoView.MediaPlayer.IsPlaying ? "Pause" : "Resume";
            favoritesToggleItem.Text = _favoritesShown ? "Hide" : "Show";
        };

        _contextMenuStrip.Closing += (_, __) => _videoView.SetCursorForCurrentView();
    }
}