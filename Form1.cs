using System.Diagnostics;
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
    private MenuTVChannelHelper _menuTVChannelHelper;
    private MenuFavoriteChannelHelper _menuFavoriteChannelHelper;

    private Channel _currentChannel = null;
    private Rectangle _manuallyAdjustedBounds = Rectangle.Empty;

    private DateTime _mouseDownLeftPrevChannel = DateTime.MinValue;
    private DateTime _mouseDownRightExit = DateTime.MinValue;

    // retry logic
    private bool _isRestarting = false;

    private const int STALL_SECONDS = 10;
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
            {
                Play(last);
            }
        };

        Logger.Info("Starting AndyTV...");

        Icon = new Icon("AndyTV.ico");

        videoView.MediaPlayer.TimeChanged += delegate
        {
            _lastActivityUtc = DateTime.UtcNow;
        };

        videoView.MediaPlayer.PositionChanged += delegate
        {
            _lastActivityUtc = DateTime.UtcNow;
        };

        videoView.MediaPlayer.Playing += delegate
        {
            _lastActivityUtc = DateTime.UtcNow;
            _isRestarting = false;

            SetCursorForCurrentMode();
            _notificationService.ShowToast(_currentChannel.DisplayName);
            RecentChannelsService.AddOrPromote(_currentChannel);
            ChannelDataService.SaveLastChannel(_currentChannel);
            _menuRecentChannelHelper?.RebuildRecentMenu();
        };

        // Configure VideoView with context menu and event handlers
        _videoView.ContextMenuStrip = _contextMenuStrip;

        _videoView.MouseDoubleClick += (_, e) =>
        {
            if (FormBorderStyle == FormBorderStyle.None)
            {
                RestoreWindow();
            }
            else
            {
                MaximizeWindow();
            }
        };

        _videoView.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _mouseDownLeftPrevChannel = DateTime.Now;
            }
            if (e.Button == MouseButtons.Right)
            {
                _mouseDownRightExit = DateTime.Now;
            }
        };

        _videoView.MouseUp += (_, e) =>
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
        };

        Controls.Add(_videoView);

        MaximizeWindow(); // start fullscreen

        ResizeEnd += delegate
        {
            if (WindowState == FormWindowState.Normal)
            {
                _manuallyAdjustedBounds = Bounds;
            }
        };

        Shown += delegate
        {
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

            _ = _menuTVChannelHelper.LoadAndBuildMenu(ChItem_Click, source.Url);

            // Cursor stuff can happen immediately on UI thread
            SetCursorForCurrentMode();

            _healthTimer = new System.Windows.Forms.Timer { Interval = 1000 }; // 1s
            _healthTimer.Tick += (_, __) =>
            {
                if (_currentChannel == null)
                {
                    return;
                }

                var nowUtc = DateTime.UtcNow;
                var inactive = nowUtc - _lastActivityUtc;

                if (inactive.TotalSeconds >= STALL_SECONDS)
                {
                    if (_isRestarting)
                    {
                        Logger.Info(
                            $"[HEALTH] Skip restart: already in progress (inactive={inactive.TotalSeconds:F0}s, threshold={STALL_SECONDS}s)."
                        );
                        return;
                    }

                    _isRestarting = true;

                    // Throttle next attempts so we don't fire again immediately
                    _lastActivityUtc = nowUtc;

                    Logger.Info(
                        $"[HEALTH] Restarting channel: '{_currentChannel.DisplayName}' url='{_currentChannel.Url}'"
                    );

                    // LibVLC ops on the pool (no UI here)
                    Play(_currentChannel);

                    // If VLC never reaches Playing, don't wedge forever — let timer try again in ~10s
                    _isRestarting = false;

                    Logger.Info(
                        "[HEALTH] Restart queued to thread pool; awaiting Playing or next health check."
                    );
                }
            };

            _healthTimer.Start();
        };
    }

    private void SetCursorForCurrentMode()
    {
        if (FormBorderStyle == FormBorderStyle.None)
        {
            _videoView.HideCursor();
        }
        else
        {
            _videoView.ShowDefault();
        }
    }

    private void ChItem_Click(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem item && item.Tag is Channel ch)
        {
            Play(ch);
        }
    }

    private void Play(Channel channel)
    {
        _currentChannel = channel;

        // UI feedback on UI thread
        _videoView.ShowWaiting();

        // Cancel any pending restart cadence and give a fresh window
        _isRestarting = false;
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
        WindowState = FormWindowState.Normal; // Set to Normal first
        Bounds = Screen.PrimaryScreen.Bounds; // Covers entire screen including taskbar area
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

        // --- Update ---
        var updateItem = new ToolStripMenuItem("Update");
        updateItem.Click += async (_, __) => await _updateService.CheckForUpdates();
        _contextMenuStrip.Items.Add(updateItem);

        // --- Swap ---
        var swapItem = new ToolStripMenuItem("Swap");
        swapItem.Click += (_, __) =>
        {
            string input = null;

            // Prefer a valid absolute URL from clipboard if present
            if (Clipboard.ContainsText())
            {
                var clip = Clipboard.GetText().Trim();
                if (Uri.IsWellFormedUriString(clip, UriKind.Absolute))
                {
                    input = clip;
                }
            }

            // If no valid clipboard URL, prompt
            if (string.IsNullOrWhiteSpace(input))
            {
                _videoView.ShowDefault();

                using var dlg = new InputForm(title: "Swap Stream", prompt: "Enter media URL:");
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                input = dlg.Result;
            }

            if (string.IsNullOrWhiteSpace(input))
                return;

            // Final sanity check
            if (!Uri.IsWellFormedUriString(input, UriKind.Absolute))
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
                _menuTVChannelHelper.ChannelByUrl(input)
                ?? new Channel { Name = "Swap", Url = input };

            Play(ch);
        };
        _contextMenuStrip.Items.Add(swapItem);

        // --- Mute / Unmute ---
        var muteItem = new ToolStripMenuItem("Mute");
        muteItem.Click += (_, __) =>
        {
            _videoView.MediaPlayer.Mute = !_videoView.MediaPlayer.Mute;
        };
        _contextMenuStrip.Items.Add(muteItem);

        // --- Logs ---
        var logsItem = new ToolStripMenuItem("Logs");
        logsItem.Click += (_, __) =>
        {
            var path = PathHelper.GetPath("logs");
            Directory.CreateDirectory(path);
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true,
                }
            );
        };
        _contextMenuStrip.Items.Add(logsItem);

        // --- Favorites ---
        var favoritesItem = new ToolStripMenuItem("Favorites");
        favoritesItem.Click += (_, __) =>
        {
            _videoView.ShowDefault();

            using Form form = new FavoriteChannelForm(_menuTVChannelHelper.Channels);
            form.FormClosed += (_, __2) =>
            {
                _menuFavoriteChannelHelper.RebuildFavoritesMenu();

                Form owner = _contextMenuStrip.SourceControl.FindForm();
                SetCursorForCurrentMode();
            };

            form.ShowDialog(_contextMenuStrip.SourceControl.FindForm());
        };
        _contextMenuStrip.Items.Add(favoritesItem);

        // --- Ad Hoc ---
        var adHocItem = new ToolStripMenuItem("Ad Hoc");
        adHocItem.Click += (_, __) =>
        {
            _videoView.ShowDefault();

            using var dialog = new AdHocChannelForm(_menuTVChannelHelper.Channels);
            dialog.ShowDialog();
            if (dialog.SelectedItem != null)
            {
                Play(dialog.SelectedItem);
            }
        };

        _contextMenuStrip.Items.Add(adHocItem);

        // ---Restart---
        var restartItem = new ToolStripMenuItem("Restart");
        restartItem.Click += (_, __) =>
        {
            Program.Restart();
        };
        _contextMenuStrip.Items.Add(restartItem);

        // --- Exit ---
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, __) => Application.Exit();
        _contextMenuStrip.Items.Add(exitItem);

        // Context menu state hooks
        _contextMenuStrip.Opening += (_, __) =>
        {
            _videoView.ShowDefault();

            muteItem.Text = _videoView.MediaPlayer.Mute ? "Unmute" : "Mute";
        };

        _contextMenuStrip.Closing += (_, __) =>
        {
            SetCursorForCurrentMode();
        };
    }
}