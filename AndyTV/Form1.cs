using System.Diagnostics;
using AndyTV.Data.Models;
using AndyTV.Helpers;
using AndyTV.Helpers.Menu;
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
    private MenuTVChannelHelper _menuTVChannelHelper;
    private MenuFavoriteChannelHelper _menuFavoriteChannelHelper;

    private Channel _currentChannel = null;
    private Rectangle _manuallyAdjustedBounds = Rectangle.Empty;

    private DateTime _mouseDownLeftPrevChannel = DateTime.MinValue;
    private DateTime _mouseDownRightExit = DateTime.MinValue;

    private bool _isRestartingStream = false;

    private const int STALL_SECONDS = 6;
    private System.Windows.Forms.Timer _healthTimer;
    private DateTime _lastActivityUtc = DateTime.UtcNow;

    public Form1(LibVLC libVLC, UpdateService updateService, VideoView videoView)
    {
        _libVLC = libVLC;
        _updateService = updateService;
        _videoView = videoView;

        InitializeComponent();

        _notificationService = new NotificationService(this);

        HandleCreated += delegate
        {
            var last = ChannelDataService.LoadLastChannel();
            if (last != null)
                Play(last);
        };

        Logger.Info("Starting AndyTV...");
        Icon = new Icon("AndyTV.ico");

        _videoView.MediaPlayer.TimeChanged += delegate
        {
            _lastActivityUtc = DateTime.UtcNow;
        };
        _videoView.MediaPlayer.PositionChanged += delegate
        {
            _lastActivityUtc = DateTime.UtcNow;
        };

        _videoView.MediaPlayer.Playing += delegate
        {
            _lastActivityUtc = DateTime.UtcNow;
            _isRestartingStream = false;

            _videoView.SetCursorForCurrentView();

            _notificationService.ShowToast(_currentChannel.DisplayName);
            RecentChannelService.AddOrPromote(_currentChannel);
            ChannelDataService.SaveLastChannel(_currentChannel);
            _menuRecentChannelHelper?.RebuildRecentMenu();
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
                && _mouseDownLeftPrevChannel.AddSeconds(1) < DateTime.Now
            )
            {
                var prevChannel = RecentChannelService.GetPrevious();
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
                && _mouseDownRightExit.AddSeconds(5) < DateTime.Now
            )
            {
                Close();
            }
        };

        Controls.Add(_videoView);
        MaximizeWindow();

        ResizeEnd += delegate
        {
            if (WindowState == FormWindowState.Normal)
                _manuallyAdjustedBounds = Bounds;
        };

        Shown += async delegate
        {
            // Prime channel cache
            await PlaylistChannelService.RefreshChannels();

            var appVersionName = "AndyTV v" + AppHelper.Version;
            Text = appVersionName;

            _menuTVChannelHelper = new MenuTVChannelHelper(_contextMenuStrip);
            BuildSettingsMenu(appVersionName);

            _menuRecentChannelHelper = new MenuRecentChannelHelper(_contextMenuStrip, ChItem_Click);
            _menuRecentChannelHelper.RebuildRecentMenu();

            _menuFavoriteChannelHelper = new MenuFavoriteChannelHelper(
                _contextMenuStrip,
                ChItem_Click
            );
            _menuFavoriteChannelHelper.RebuildFavoritesMenu();

            // If no valid playlist, open manager; if saved, refresh + rebuild
            if (PlaylistChannelService.Load().Count == 0)
            {
                await HandlePlaylistManager();
            }

            // Initial menu build
            await _menuTVChannelHelper.RebuildMenu(ChItem_Click);

            _videoView.SetCursorForCurrentView();

            _healthTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _healthTimer.Tick += (_, __) =>
            {
                if (_currentChannel == null)
                    return;

                var nowUtc = DateTime.UtcNow;
                var inactive = nowUtc - _lastActivityUtc;

                if (inactive.TotalSeconds >= STALL_SECONDS)
                {
                    if (_isRestartingStream)
                    {
                        Logger.Info(
                            $"[HEALTH] Skip restart: already in progress (inactive={inactive.TotalSeconds:F0}s)."
                        );
                        return;
                    }

                    _isRestartingStream = true;
                    _lastActivityUtc = nowUtc;

                    Logger.Info(
                        $"[HEALTH] Restarting channel: '{_currentChannel.DisplayName}' url='{_currentChannel.Url}'"
                    );
                    Play(_currentChannel);
                    _isRestartingStream = false;
                    Logger.Info("[HEALTH] Restart queued; awaiting Playing or next health check.");
                }
            };

            _healthTimer.Start();
        };
    }

    // Reusable: open Playlist Manager; if dlg.Saved, refresh + rebuild
    private async Task HandlePlaylistManager()
    {
        _videoView.ShowDefault();
        using (var dlg = new PlaylistManagerForm())
        {
            dlg.ShowDialog(this);
            if (dlg.Saved)
            {
                await _menuTVChannelHelper.RebuildMenu(ChItem_Click);
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
        _isRestartingStream = false;
        _lastActivityUtc = DateTime.UtcNow;

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

    private void BuildSettingsMenu(string appVersionName)
    {
        var header = MenuHelper.AddHeader(_contextMenuStrip, appVersionName);
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

        // ===== Channels =====
        var channelsMenu = new ToolStripMenuItem("Channels");

        var adHocItem = new ToolStripMenuItem("Ad Hoc");
        adHocItem.Click += (_, __) =>
        {
            _videoView.ShowDefault();
            using var dialog = new AdHocChannelForm(PlaylistChannelService.Channels);
            dialog.ShowDialog();
            if (dialog.SelectedItem != null)
                Play(dialog.SelectedItem);
        };
        channelsMenu.DropDownItems.Add(adHocItem);

        var swapItem = new ToolStripMenuItem("Swap");
        swapItem.Click += (_, __) =>
        {
            string input = null;

            if (Clipboard.ContainsText())
            {
                var clip = Clipboard.GetText().Trim();
                if (Uri.IsWellFormedUriString(clip, UriKind.Absolute))
                    input = clip;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                _videoView.ShowDefault();
                using var dlg = new InputForm("Swap Stream", "Enter media URL:");
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                input = dlg.Result;
            }

            if (
                string.IsNullOrWhiteSpace(input)
                || !Uri.IsWellFormedUriString(input, UriKind.Absolute)
            )
            {
                MessageBox.Show(
                    this,
                    "Please enter a valid absolute URL.",
                    "Invalid URL",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            var ch =
                MenuTVChannelHelper.ChannelByUrl(input)
                ?? new Channel { Name = "Swap", Url = input };
            Play(ch);
        };
        channelsMenu.DropDownItems.Add(swapItem);

        channelsMenu.DropDownItems.Add(new ToolStripSeparator());

        var playlistsItem = new ToolStripMenuItem("Manage");
        playlistsItem.Click += async (_, __) => await HandlePlaylistManager();
        channelsMenu.DropDownItems.Add(playlistsItem);

        // add a "Refresh Channels" item under it
        var refreshItem = new ToolStripMenuItem("Refresh");
        refreshItem.Click += async (_, __) =>
        {
            await _menuTVChannelHelper.RebuildMenu(ChItem_Click);
        };
        channelsMenu.DropDownItems.Add(refreshItem);

        _contextMenuStrip.Items.Add(channelsMenu);

        // ===== Favorites =====
        var favoritesMenu = new ToolStripMenuItem("Favorites");

        void OpenFavorites(Channel addOnOpen = null)
        {
            _videoView.ShowDefault();
            using var form = new FavoriteChannelForm(PlaylistChannelService.Channels, addOnOpen);
            form.FormClosed += (_, __) =>
            {
                if (form.Saved)
                    _menuFavoriteChannelHelper.RebuildFavoritesMenu();
                _videoView.SetCursorForCurrentView();
            };
            form.ShowDialog();
        }

        var favoritesManageItem = new ToolStripMenuItem("Manage");
        favoritesManageItem.Click += (_, __) => OpenFavorites();
        favoritesMenu.DropDownItems.Add(favoritesManageItem);

        var favoritesAddCurrentItem = new ToolStripMenuItem("Add Playing");
        favoritesAddCurrentItem.Click += (_, __) =>
        {
            if (!MenuFavoriteChannelHelper.IsDuplicate(_currentChannel))
            {
                OpenFavorites(_currentChannel);
            }
        };
        favoritesMenu.DropDownItems.Add(favoritesAddCurrentItem);

        _contextMenuStrip.Items.Add(favoritesMenu);

        // ===== App =====
        var appMenu = new ToolStripMenuItem("App");

        var updateItem = new ToolStripMenuItem("Update");
        updateItem.Click += async (_, __) => await _updateService.CheckForUpdates();
        appMenu.DropDownItems.Add(updateItem);

        var logsItem = new ToolStripMenuItem("Logs");
        logsItem.Click += (_, __) =>
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = Logger.LogFolder,
                    UseShellExecute = true,
                }
            );
        };
        appMenu.DropDownItems.Add(logsItem);

        var muteItem = new ToolStripMenuItem("Mute");
        muteItem.Click += (_, __) =>
        {
            _videoView.MediaPlayer.Mute = !_videoView.MediaPlayer.Mute;
        };
        appMenu.DropDownItems.Add(muteItem);

        appMenu.DropDownItems.Add(new ToolStripSeparator());

        var restartItem = new ToolStripMenuItem("Restart");
        restartItem.Click += (_, __) => Program.Restart();
        appMenu.DropDownItems.Add(restartItem);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, __) => Application.Exit();
        appMenu.DropDownItems.Add(exitItem);

        _contextMenuStrip.Items.Add(appMenu);

        _contextMenuStrip.Opening += (_, __) =>
        {
            _videoView.ShowDefault();
            muteItem.Text = _videoView.MediaPlayer.Mute ? "Unmute" : "Mute";
        };

        _contextMenuStrip.Closing += (_, __) => _videoView.SetCursorForCurrentView();
    }
}