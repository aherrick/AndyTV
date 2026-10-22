using System.Diagnostics;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace AndyTV;

internal sealed class PlayerForm : Form
{
    private static string AppVersionName => $"AndyTV {Application.ProductVersion}";

    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private readonly VideoView _videoView;
    private readonly ContextMenuStrip _menu = new();
    private readonly List<ToolStripItem> _recentItems = [];
    private readonly ToolStripSeparator _recentSeparator = new();
    private readonly ToolStripSeparator _favoritesSeparator = new();

    private readonly LocalConfigService _configService;
    private readonly LocalConfig _config;
    private readonly PlaylistService _playlistService;
    private readonly RecentChannelService _recentService;
    private readonly LastChannelService _lastService;
    private readonly FavoriteChannelService _favoriteService;
    private readonly ToolStripMenuItem _muteItem = new("Mute");
    private readonly ToolStripMenuItem _addFavoriteItem = new("Add Current to Favorites");
    private List<Playlist> _playlists = [];

    private Channel _current;
    private Channel _pending;

    // Channel-tree items (playlists + Top US/UK/24-7) currently in the menu. Built off
    // the UI thread and swapped in, so the menu is usable before they finish loading.
    private readonly List<ToolStripItem> _channelItems = [];

    // Spinner shows while channels are loading (_busy) or a channel is connecting (_pending).
    private bool _busy;

    private bool _menuOpen;
    private Form _toast;
    private DateTime _leftDown = DateTime.MinValue;
    private DateTime _rightDown = DateTime.MinValue;
    private FormWindowState _restoreState = FormWindowState.Maximized;
    private Rectangle _restoreBounds;

    private const int LeftHoldSeconds = 1;
    private const int RightHoldSeconds = 5;

    private readonly CancellationTokenSource _cts = new();
    private readonly StreamHealthMonitor _healthMonitor = new();
    private volatile bool _videoLate;
    private readonly System.Windows.Forms.Timer _healthTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _sleepTimer = new();
    private ToolStripMenuItem _sleepOffItem;

    // LibVLC play/stop block for seconds (vout teardown, network probing), which deadlocks the
    // WinForms message loop. All callers are on the UI thread, so chaining keeps them in order.
    private Task _playback = Task.CompletedTask;

    public PlayerForm()
    {
        var storage = new LocalStorageProvider();
        _configService = new LocalConfigService(storage);
        _config = _configService.Load();
        _playlistService = new PlaylistService(storage);
        _recentService = new RecentChannelService(storage);
        _lastService = new LastChannelService(storage);
        _favoriteService = new FavoriteChannelService(storage);

        Text = AppVersionName;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        BackColor = Color.Black;

        // Decode mode and video output are fixed at LibVLC creation, so changing either requires an app restart.
        _libVLC = new LibVLC(
            _config.DisableHardwareAcceleration ? "--avcodec-hw=none" : "--avcodec-hw=any",
            // ",any" falls back to automatic when the chosen output isn't available.
            _config.VideoOutput is { } vout ? $"--vout={vout},any" : "--vout=any"
        );

        // Native VLC breadcrumbs (decoder/network errors) that often precede a hard crash.
        _libVLC.Log += OnLibVlcLog;

        _mediaPlayer = new MediaPlayer(_libVLC)
        {
            EnableMouseInput = false,
            EnableKeyInput = false,
        };

        _healthTimer.Tick += OnHealthTick;
        _sleepTimer.Tick += OnSleepTimer;

        _mediaPlayer.Playing += OnPlaying;

        _muteItem.Click += (_, _) => _mediaPlayer.Mute = !_mediaPlayer.Mute;
        _addFavoriteItem.Click += (_, _) => AddCurrentFavorite();

        _videoView = new VideoView
        {
            Dock = DockStyle.Fill,
            MediaPlayer = _mediaPlayer,
            BackColor = Color.Black,
            ContextMenuStrip = _menu,
        };
        // Mouse gestures: double-click = fullscreen, left long-press = previous channel,
        // middle = mute, right long-press = exit, wheel = channel up/down.
        _videoView.MouseDoubleClick += OnVideoDoubleClick;
        _videoView.MouseDown += OnVideoMouseDown;
        _videoView.MouseUp += OnVideoMouseUp;
        _videoView.MouseWheel += OnVideoMouseWheel;
        Controls.Add(_videoView);

        _menu.Opening += (_, _) =>
        {
            _menuOpen = true;
            _muteItem.Text = _mediaPlayer.Mute ? "Unmute" : "Mute";
            UpdateCursor();
        };
        _menu.Closing += (_, _) =>
        {
            _menuOpen = false;
            UpdateCursor();
        };
        KeyPreview = true;
        KeyDown += OnFormKeyDown;
        Shown += OnFormShown;
    }

    private void OnVideoDoubleClick(object sender, MouseEventArgs e)
    {
        if (this.IsFullscreen())
        {
            this.ExitFullscreen(_restoreState, _restoreBounds);
        }
        else
        {
            _restoreState = WindowState;
            _restoreBounds = Bounds;
            this.EnterFullscreen();
        }
        UpdateCursor();
    }

    private void OnVideoMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _leftDown = DateTime.Now;
        }
        else if (e.Button == MouseButtons.Right)
        {
            _rightDown = DateTime.Now;
        }
    }

    private void OnVideoMouseUp(object sender, MouseEventArgs e)
    {
        if (
            e.Button == MouseButtons.Left
            && _leftDown != DateTime.MinValue
            && _leftDown.AddSeconds(LeftHoldSeconds) < DateTime.Now
            && _recentService.GetPrevious() is { } previous
        )
        {
            Play(previous);
        }
        else if (e.Button == MouseButtons.Middle)
        {
            _mediaPlayer.Mute = !_mediaPlayer.Mute;
        }
        else if (
            e.Button == MouseButtons.Right
            && _rightDown != DateTime.MinValue
            && _rightDown.AddSeconds(RightHoldSeconds) < DateTime.Now
        )
        {
            Close();
        }
        _leftDown = DateTime.MinValue;
        _rightDown = DateTime.MinValue;
    }

    private void OnVideoMouseWheel(object sender, MouseEventArgs e)
    {
        var direction = e.Delta > 0 ? 1 : -1;
        if (_recentService.GetRelative(_current?.Url, direction) is { } next)
        {
            Play(next);
        }
    }

    private void OnFormKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
    }

    private async void OnFormShown(object sender, EventArgs e)
    {
        if (Program.StartOnRight)
        {
            this.SnapToHalf(left: false);
        }
        else
        {
            this.EnterFullscreen();
        }
        _healthTimer.Start();
        await Initialize();
    }

    private async Task Initialize()
    {
        // Play first — it only needs local storage, so playback never waits on the menu or playlists.
        if (_lastService.LoadLastChannel() is { } last)
        {
            Play(last);
        }

        BuildStaticMenu();
        _playlists = _playlistService.LoadPlaylists();
        await RefreshChannels();

        _ = RunHourlyRefresh();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UpdateCursor();
    }

    // Single place that decides the cursor: visible while the menu is open, otherwise
    // the spinner while channels load (_busy) or a channel connects (_pending), and
    // hidden (fullscreen) / default (windowed) when idle.
    private void UpdateCursor()
    {
        if (_menuOpen)
        {
            _videoView.ShowDefault();
        }
        else if (_busy || _pending is not null)
        {
            _videoView.ShowWaiting();
        }
        else
        {
            _videoView.SetCursorForCurrentView();
        }
    }

    private async Task RunHourlyRefresh()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        // Dispose on shutdown so the wait returns false instead of throwing (no debugger break).
        await using var registration = _cts.Token.Register(timer.Dispose);
        while (await timer.WaitForNextTickAsync())
        {
            try
            {
                await RefreshChannels();
                Logger.Info("[REFRESH] Hourly channel refresh complete");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Hourly refresh failed");
            }
        }
    }

    private async Task ManagePlaylists()
    {
        using var form = new PlaylistManagerForm(_playlists);
        form.ShowDialog(this);
        if (!form.Changed)
        {
            return;
        }
        _playlistService.SavePlaylists(_playlists);
        await RefreshChannels();
    }

    private void SearchChannels()
    {
        using var form = new SearchForm(_playlistService.Channels);
        if (form.ShowDialog(this) == DialogResult.OK && form.Selected is { } channel)
        {
            Play(channel);
        }
    }

    private void ShowGuide()
    {
        // The first GuideForm construction blocks the UI thread while WebView2/Syncfusion load.
        Cursor.Current = Cursors.WaitCursor;
        using var form = new GuideForm();
        if (form.ShowDialog(this) == DialogResult.OK
            && form.SelectedStreamingTvId is { } id
            && ResolveByStreamingTvId(id) is { } channel)
        {
            Play(channel);
        }
        UpdateCursor();
    }

    private Channel ResolveByStreamingTvId(string streamingTvId)
    {
        var entries = ChannelService.TopUs().Values
            .Concat(ChannelService.TopUk().Values)
            .SelectMany(e => e)
            .Where(e => string.Equals(e.StreamingTVId, streamingTvId, StringComparison.OrdinalIgnoreCase));
        foreach (var entry in entries)
        {
            var matches = ChannelMatcher.MatchTop(entry, _playlistService.UsUkChannels);
            if (matches.Count > 0)
            {
                return matches[0];
            }
        }
        return null;
    }

    private void BuildStaticMenu()
    {
        _menu.Items.Clear();
        _channelItems.Clear();

        var header = new ToolStripMenuItem(AppVersionName);
        header.Click += (_, _) => OpenUrl(UpdateService.RepoUrl);
        _menu.Items.Add(header);
        _menu.Items.Add(new ToolStripSeparator());

        var manage = new ToolStripMenuItem("Manage");
        manage.DropDownItems.Add("Search\u2026", null, (_, _) => SearchChannels());
        manage.DropDownItems.Add("Guide", null, (_, _) => ShowGuide());
        manage.DropDownItems.Add(
            "Watchlist",
            null,
            (_, _) => OpenUrl("https://andytv.today/")
        );
        manage.DropDownItems.Add(new ToolStripSeparator());
        manage.DropDownItems.Add("Playlists\u2026", null, async (_, _) => await ManagePlaylists());
        manage.DropDownItems.Add("Refresh", null, async (_, _) => await RefreshChannels());
        manage.DropDownItems.Add(new ToolStripSeparator());
        manage.DropDownItems.Add(_addFavoriteItem);
        manage.DropDownItems.Add("Favorites\u2026", null, (_, _) => ManageFavorites());
        manage.DropDownItems.Add(new ToolStripSeparator());
        manage.DropDownItems.Add(
            "Check for Updates",
            null,
            async (_, _) =>
            {
                SetBusy(true);
                await UpdateService.Check();
                SetBusy(false);
            }
        );
        manage.DropDownItems.Add("Logs", null, (_, _) => OpenUrl(Logger.LogFolder));
        manage.DropDownItems.Add(BuildAdvancedMenu());
        manage.DropDownItems.Add(_muteItem);
        manage.DropDownItems.Add(new ToolStripSeparator());
        manage.DropDownItems.AddRange(BuildSleepMenus());
        manage.DropDownItems.Add(new ToolStripSeparator());
        manage.DropDownItems.Add("New Window", null, (_, _) => NewWindow());
        manage.DropDownItems.Add(new ToolStripSeparator());
        manage.DropDownItems.Add("Exit", null, (_, _) => Close());
        _menu.Items.Add(manage);
        _menu.Items.Add(new ToolStripSeparator());

        // Recent sits above the favorites list, separated only when both exist.
        _menu.Items.Add(_favoritesSeparator);
        _menu.Items.Add(_recentSeparator);
        RebuildFavorites();
    }

    private ToolStripMenuItem BuildAdvancedMenu()
    {
        var advanced = new ToolStripMenuItem("Advanced");
        advanced.DropDownItems.Add(RestartOptionMenu(
            "Hardware Acceleration",
            [("Auto", false), ("Disabled", true)],
            _config.DisableHardwareAcceleration,
            v => _config.DisableHardwareAcceleration = v));
        advanced.DropDownItems.Add(RestartOptionMenu(
            "Video Output",
            [("Auto", null), ("Direct3D11", "direct3d11"), ("OpenGL", "glwin32")],
            _config.VideoOutput,
            v => _config.VideoOutput = v));

        var buffer = new ToolStripMenuItem("Network Buffer");
        (string Label, int? Milliseconds)[] presets =
        [
            ("Default (VLC)", null),
            ("None (0 ms)", 0),
            ("Low Latency (300 ms)", 300),
            ("Extra Buffer (2000 ms)", 2000),
            ("High Stability (5000 ms)", 5000),
        ];
        foreach (var (label, milliseconds) in presets)
        {
            var item = new ToolStripMenuItem(label)
            {
                Checked = _config.NetworkBufferMilliseconds == milliseconds,
            };
            item.Click += (_, _) =>
            {
                _config.NetworkBufferMilliseconds = milliseconds;
                _configService.Save(_config);
                CheckOnly(item);
            };
            buffer.DropDownItems.Add(item);
        }
        buffer.DropDownItems.Add(new ToolStripSeparator());
        buffer.DropDownItems.Add(new ToolStripMenuItem("Applies on next channel change") { Enabled = false });
        advanced.DropDownItems.Add(buffer);
        return advanced;
    }

    // Radio options baked into LibVLC at startup, so picking a new one saves and restarts.
    private ToolStripMenuItem RestartOptionMenu<TValue>(
        string title,
        (string Label, TValue Value)[] options,
        TValue current,
        Action<TValue> apply)
    {
        var menu = new ToolStripMenuItem(title);
        foreach (var (label, value) in options)
        {
            var item = new ToolStripMenuItem(label)
            {
                Checked = EqualityComparer<TValue>.Default.Equals(current, value),
            };
            item.Click += (_, _) =>
            {
                if (item.Checked || MessageBox.Show(
                    this,
                    $"AndyTV will restart to apply {title}. Continue?",
                    title,
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question
                ) != DialogResult.OK)
                {
                    return;
                }
                apply(value);
                _configService.Save(_config);
                Application.Restart();
            };
            menu.DropDownItems.Add(item);
        }
        return menu;
    }

    // Sleep length is per session (starts Off); what happens when it fires is saved.
    private ToolStripItem[] BuildSleepMenus()
    {
        var timer = new ToolStripMenuItem("Sleep Timer");
        int[] presets = [0, 15, 30, 60, 90, 120];
        foreach (var minutes in presets)
        {
            var item = new ToolStripMenuItem(minutes == 0 ? "Off" : $"{minutes} min")
            {
                Checked = minutes == 0,
            };
            item.Click += (_, _) =>
            {
                _sleepTimer.Stop();
                if (minutes > 0)
                {
                    _sleepTimer.Interval = minutes * 60_000;
                    _sleepTimer.Start();
                }
                CheckOnly(item);
            };
            timer.DropDownItems.Add(item);
        }
        _sleepOffItem = (ToolStripMenuItem)timer.DropDownItems[0];

        var action = new ToolStripMenuItem("When Timer Ends");
        (string Label, SleepAction Value)[] actions =
        [
            ("Close App", SleepAction.CloseApp),
            ("Stop Playback", SleepAction.StopPlayback),
            ("Mute", SleepAction.Mute),
        ];
        foreach (var (label, value) in actions)
        {
            var item = new ToolStripMenuItem(label) { Checked = _config.SleepAction == value };
            item.Click += (_, _) =>
            {
                _config.SleepAction = value;
                _configService.Save(_config);
                CheckOnly(item);
            };
            action.DropDownItems.Add(item);
        }

        return [timer, action];
    }

    private void OnSleepTimer(object sender, EventArgs e)
    {
        _sleepTimer.Stop();
        CheckOnly(_sleepOffItem);
        if (_config.SleepAction == SleepAction.Mute)
        {
            _mediaPlayer.Mute = true;
        }
        else if (_config.SleepAction == SleepAction.StopPlayback)
        {
            // Clearing _current also keeps the health check from restarting the stream.
            _current = null;
            _pending = null;
            UpdateCursor();
            RunPlayback(() => _mediaPlayer.Stop());
        }
        else
        {
            Close();
        }
    }

    // Radio-style check within a submenu.
    private static void CheckOnly(ToolStripMenuItem item)
    {
        if (item.OwnerItem is not ToolStripMenuItem parent)
        {
            return;
        }
        foreach (var option in parent.DropDownItems.OfType<ToolStripMenuItem>())
        {
            option.Checked = option == item;
        }
    }

    // Downloads/parses channels and builds the channel tree off the UI thread (only the
    // final Add/Remove swap runs on the UI thread), so the menu never freezes on load.
    private async Task RefreshChannels()
    {
        SetBusy(true);
        try
        {
            var items = await Task.Run(async () =>
            {
                await _playlistService.RefreshChannelsAsync();
                return BuildChannelItems();
            });
            SwapChannelItems(items);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SwapChannelItems(List<ToolStripItem> items)
    {
        // Not SuspendPainting(): its WM_SETREDRAW(TRUE) shows the hidden menu.
        _menu.SuspendLayout();
        try
        {
            foreach (var item in _channelItems)
            {
                _menu.Items.Remove(item);
            }
            _channelItems.Clear();
            foreach (var item in items)
            {
                _menu.Items.Add(item);
                _channelItems.Add(item);
            }
        }
        finally
        {
            _menu.ResumeLayout();
        }
    }

    // Runs off the UI thread; only creates ToolStripItems (safe until added to the menu).
    private List<ToolStripItem> BuildChannelItems()
    {
        List<ToolStripItem> items = [];

        foreach (
            var (playlist, channels) in _playlistService.PlaylistChannels.Where(x =>
                x.Playlist.ShowInMenu
            )
        )
        {
            var item = new ToolStripMenuItem(playlist.Name) { Enabled = channels.Count > 0 };
            item.DropDownItems.AddRange([.. ChannelMatcher.BuildPlaylistNodes(playlist, channels).Select(RenderItem)]);
            items.Add(item);
        }

        // US/UK match only playlists flagged for it, so TV-show/movie playlists don't
        // pollute the curated lists; 24-7 still spans all channels.
        var usUkChannels = _playlistService.UsUkChannels;
        var allChannels = _playlistService.Channels;
        items.Add(Render(ChannelMatcher.BuildTopRegion("US", ChannelService.TopUs(), usUkChannels)));
        items.Add(Render(ChannelMatcher.BuildTopRegion("UK", ChannelService.TopUk(), usUkChannels)));
        items.Add(Render(ChannelMatcher.BuildTopRegion("Global", ChannelService.TopGlobal(), allChannels)));
        var menu247 = Render(ChannelMatcher.Build247(allChannels));
        if (menu247.DropDownItems.Count > 0)
        {
            items.Add(menu247);
        }

        return items;
    }

    // Rebuilds only the favorites section (between the two separators) in place,
    // so adding/editing a favorite never rebuilds the playlist channel tree.
    private void RebuildFavorites()
    {
        var index = _menu.Items.IndexOf(_favoritesSeparator) + 1;
        while (index < _menu.Items.Count && _menu.Items[index] != _recentSeparator)
        {
            _menu.Items.RemoveAt(index);
        }

        foreach (
            var fav in _favoriteService.Favorites.Where(f => string.IsNullOrWhiteSpace(f.Group))
        )
        {
            _menu.Items.Insert(index++, ChannelItem(fav.DisplayName, fav));
        }
        foreach (
            var group in _favoriteService
                .Favorites.Where(f => !string.IsNullOrWhiteSpace(f.Group))
                .GroupBy(f => f.Group, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            var groupItem = new ToolStripMenuItem(group.Key);
            groupItem.DropDownItems.AddRange([.. group.Select(fav => ChannelItem(fav.DisplayName, fav))]);
            _menu.Items.Insert(index++, groupItem);
        }
        RefreshRecent();
    }

    private ToolStripMenuItem Render(MenuNode node)
    {
        if (node.Channel is { } channel)
        {
            return ChannelItem(node.Text, channel);
        }

        var item = new ToolStripMenuItem(node.Text);
        if (node.Children is { Count: > 0 } children)
        {
            item.DropDownItems.AddRange([.. children.Select(RenderItem)]);
        }
        return item;
    }

    private ToolStripItem RenderItem(MenuNode node) =>
        node.IsSeparator ? new ToolStripSeparator() : Render(node);

    private ToolStripMenuItem ChannelItem(string text, Channel channel)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) => Play(channel);
        return item;
    }

    private void AddCurrentFavorite()
    {
        if (_current is { } current && !_favoriteService.IsFavorite(current))
        {
            // Favorites start ungrouped; the user assigns a Group in the manager.
            _favoriteService.AddFavorite(
                new Channel
                {
                    RawName = current.RawName,
                    Name = current.Name,
                    MappedName = current.MappedName,
                    Url = current.Url,
                    LogoUrl = current.LogoUrl,
                }
            );
            RebuildFavorites();
        }
    }

    private void ManageFavorites()
    {
        var favorites = _favoriteService.LoadFavoriteChannels();
        using var form = new FavoritesManagerForm(favorites);
        form.ShowDialog(this);
        if (!form.Changed)
        {
            return;
        }
        _favoriteService.SaveFavoriteChannels(favorites);
        RebuildFavorites();
    }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    // Snap this window to the left half and launch a second instance on the right.
    private void NewWindow()
    {
        if (this.IsFullscreen())
        {
            this.SnapToHalf(left: true);
            UpdateCursor();
        }
        Process.Start(
            new ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                Arguments = "--new-instance --right",
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory,
            }
        );
    }

    private void RefreshRecent()
    {
        foreach (var item in _recentItems)
        {
            _menu.Items.Remove(item);
        }
        _recentItems.Clear();

        var recents = _recentService.GetRecentChannels();
        var index = _menu.Items.IndexOf(_favoritesSeparator);
        foreach (var recent in recents)
        {
            var leaf = ChannelItem(recent.DisplayName, recent);
            _menu.Items.Insert(index++, leaf);
            _recentItems.Add(leaf);
        }

        var hasFavorites = _favoriteService.Favorites.Count > 0;
        _favoritesSeparator.Visible = recents.Count > 0 && hasFavorites;
        // Divider hidden only when nothing sits above it (no recents and no favorites).
        _recentSeparator.Visible = recents.Count > 0 || hasFavorites;
    }

    private async void OnHealthTick(object sender, EventArgs e)
    {
        // Skip while VLC is still busy so checks and restarts never pile up behind a slow call.
        if (_current is not { } current || !_playback.IsCompleted)
        {
            return;
        }

        MediaStats stats = default;
        RunPlayback(() =>
        {
            using var media = _mediaPlayer.Media;
            if (media is not null)
            {
                stats = media.Statistics;
            }
        });
        await _playback;

        if (
            !IsDisposed
            && current == _current
            && (_videoLate || _healthMonitor.IsStalled(stats.PlayedAudioBuffers, stats.DisplayedPictures))
        )
        {
            Logger.Warn($"[HEALTH] No playback progress, restarting {current.DisplayName}");
            Play(current);
        }
    }

    private void RunPlayback(Action action)
    {
        _playback = _playback.ContinueWith(
            _ =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Playback failed");
                }
            },
            TaskScheduler.Default
        );
    }

    private void Play(Channel channel)
    {
        _current = channel;
        _pending = channel;
        _healthMonitor.Reset();
        UpdateCursor();
        var bufferMilliseconds = _config.NetworkBufferMilliseconds;
        RunPlayback(() =>
        {
            _mediaPlayer.Stop();
            _videoLate = false;
            using var media = new Media(_libVLC, new Uri(channel.Url));
            if (bufferMilliseconds is >= 0 and <= 60000)
            {
                media.AddOption($":network-caching={bufferMilliseconds.Value}");
            }
            _mediaPlayer.Play(media);
        });
    }

    private void OnPlaying(object sender, EventArgs e)
    {
        if (_pending is not { } played)
        {
            return;
        }
        _pending = null;

        // Raised on a VLC thread.
        BeginInvoke(() => CommitRecent(played));
    }

    private void CommitRecent(Channel played)
    {
        _recentService.AddOrPromote(played);
        _lastService.SaveLastChannel(played);
        UpdateCursor();
        RefreshRecent();
        ShowNowPlaying(played.DisplayName);
    }

    // Brief "now playing" toast, replacing the previous one at each channel change.
    private void ShowNowPlaying(string text)
    {
        _toast?.Close();
        var toast = Toast.Show(this, text);
        _toast = toast;
        toast.FormClosed += (_, _) =>
        {
            if (_toast == toast)
            {
                _toast = null;
            }
        };
    }

    // Runs on VLC's decoder threads: log sparingly, since warnings flood at hundreds per second and
    // the synchronous file writes slow decoding further.
    private void OnLibVlcLog(object _, LogEventArgs e)
    {
        // VLC has no event for this and rarely recovers; logged per frame, so flag instead of writing.
        if (e.Message.Contains("late video", StringComparison.Ordinal))
        {
            _videoLate = true;
            return;
        }

        // Once per stream; absent means VLC fell back to software decoding.
        if (e.Message.Contains("for hardware decoding", StringComparison.Ordinal))
        {
            Logger.Info($"[VLC/{e.Module}] {e.Message}");
        }
        else if (e.Level is LogLevel.Error)
        {
            Logger.Error($"[VLC/{e.Module}] {e.Message}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts.Cancel();
            _cts.Dispose();
            _healthTimer.Tick -= OnHealthTick;
            _healthTimer.Dispose();
            _sleepTimer.Dispose();
            _mediaPlayer.Playing -= OnPlaying;
            _libVLC.Log -= OnLibVlcLog;

            // Detach the video output first: tearing VLC down while it still owns the VideoView
            // window makes it wait on the UI thread, which is the thread doing the teardown.
            _videoView.MediaPlayer = null;
            RunPlayback(() =>
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
                _libVLC.Dispose();
            });
        }
        base.Dispose(disposing);
    }
}
