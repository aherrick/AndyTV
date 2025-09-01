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

    // Cancels any pending waits/retries when a new Play begins
    private CancellationTokenSource _playCts;

    // Prevent multiple restart loops
    private bool _isRestarting = false;

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
                Play(last);
            }
        };

        Logger.Info("Starting AndyTV...");

        Icon = new Icon("AndyTV.ico");

        // ------- Lean Restart (3 tries; each waits up to 10s for Playing; aborts if user force-switches)
        void RestartChannel(string trigger)
        {
            if (_isRestarting)
                return; // Prevent multiple restart loops

            Task.Run(async () =>
            {
                _isRestarting = true;
                try
                {
                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        Logger.Info(
                            $"[RESTART] trigger={trigger} attempt={attempt}/3 ch='{_currentChannel?.DisplayName}'"
                        );

                        var tcs = new TaskCompletionSource<bool>(
                            TaskCreationOptions.RunContinuationsAsynchronously
                        );
                        void OnPlaying(object s, EventArgs e)
                        {
                            tcs.TrySetResult(true);
                        }

                        _videoView.MediaPlayer.Playing += OnPlaying;
                        try
                        {
                            // Use BeginInvoke to marshal to UI thread
                            BeginInvoke(() => Play(_currentChannel));

                            // Play() recreated _playCts; use the fresh token for this attempt
                            var attemptToken = _playCts.Token;

                            // Wait up to 10s for Playing, or cancel if user force-plays another channel
                            var finished = await Task.WhenAny(
                                tcs.Task,
                                Task.Delay(10_000, attemptToken)
                            );

                            if (attemptToken.IsCancellationRequested)
                            {
                                return; // user switched channels; this retry loop is stale
                            }

                            if (finished == tcs.Task && await tcs.Task)
                            {
                                Logger.Info($"[RESTART] success on attempt {attempt}");
                                return; // hit Playing — done
                            }

                            Logger.Info($"[RESTART] no 'Playing' within 10s on attempt {attempt}");
                        }
                        finally
                        {
                            _videoView.MediaPlayer.Playing -= OnPlaying;
                        }
                    }

                    Logger.Warn(
                        $"[RESTART] giving up after 3 attempts ch='{_currentChannel?.DisplayName}'"
                    );
                }
                catch (OperationCanceledException)
                {
                    // stale restart; ignore
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "[RESTART] unexpected error");
                }
                finally
                {
                    _isRestarting = false; // Reset flag when done
                }
            });
        }

        // ------- VLC events (lean)
        videoView.MediaPlayer.Playing += delegate
        {
            if (_playCts.IsCancellationRequested)
            {
                return; // stale event from old play
            }

            _isRestarting = false; // Reset restart flag on successful play

            // Reset cursor based on fullscreen state
            SetCursorForCurrentMode();

            BeginInvoke(() =>
            {
                _notificationService.ShowToast(_currentChannel.DisplayName);
                RecentChannelsService.AddOrPromote(_currentChannel);
                ChannelDataService.SaveLastChannel(_currentChannel);
                _menuRecentChannelHelper?.RebuildRecentMenu();
            });
        };

        videoView.MediaPlayer.EndReached += (_, __) =>
        {
            if (_playCts.IsCancellationRequested)
            {
                return;
            } // stale
            RestartChannel("EndReached");
        };

        videoView.MediaPlayer.EncounteredError += (_, __) =>
        {
            if (_playCts.IsCancellationRequested)
            {
                return;
            } // stale
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

            Logger.Info("[CHANNELS] Loading from M3U...");
            _videoView.ShowWaiting();

            // Only the slow channel loading runs on background thread
            _ = Task.Run(async () =>
            {
                await _menuTVChannelHelper.LoadChannels(ChItem_Click, source.Url);
                Logger.Info("[CHANNELS] Loaded");
            });

            // Cursor stuff can happen immediately on UI thread
            SetCursorForCurrentMode();
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
            Play(ch); // manual selection overrides any pending waits/retries
        }
    }

    private void Play(Channel channel)
    {
        // Cancel any pending work from previous play (retries or waits)
        _playCts.Cancel();
        _playCts.Dispose();
        _playCts = new CancellationTokenSource();

        _isRestarting = false; // Reset restart flag when user manually plays

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

    private void VideoView_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _mouseDownLeftPrevChannel = DateTime.Now;
        }
        if (e.Button == MouseButtons.Right)
        {
            _mouseDownRightExit = DateTime.Now;
        }
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
                Play(prevChannel); // authoritative previous-channel switch
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
        {
            RestoreWindow();
        }
        else
        {
            MaximizeWindow();
        }
    }

    private void BuildSettingsMenu(string appVersionName)
    {
        // Header
        var header = MenuHelper.AddHeader(_contextMenuStrip, appVersionName);

        // --- Update ---
        var updateItem = new ToolStripMenuItem("Update");
        updateItem.Click += async (_, __) => await _updateService.CheckForUpdates();
        _contextMenuStrip.Items.Add(updateItem);

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

                using var dlg = new InputForm("Swap Stream", "Enter media URL:");
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                input = dlg.Result?.Trim();
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

        // ---Restart-- -
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
            if (_videoView.MediaPlayer != null)
            {
                muteItem.Text = _videoView.MediaPlayer.Mute ? "Unmute" : "Mute";
            }
        };

        _contextMenuStrip.Closing += (_, __) =>
        {
            SetCursorForCurrentMode();
        };
    }
}