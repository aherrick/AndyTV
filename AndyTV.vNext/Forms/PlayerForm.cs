using System.Diagnostics;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace AndyTV.vNext;

internal sealed class PlayerForm : Form
{
    private readonly LibVLC _libVLC = new();
    private readonly MediaPlayer _mediaPlayer;
    private readonly VideoView _videoView;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem[] _recentItems = new ToolStripMenuItem[5];
    private readonly ToolStripSeparator _recentSeparator = new();
    private readonly ToolStripSeparator _favoritesSeparator = new();

    private readonly IStorageProvider _storage = new LocalStorageProvider();
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
    private readonly StreamHealthMonitor _healthMonitor;
    private readonly System.Windows.Forms.Timer _healthTimer = new() { Interval = 1000 };

    public PlayerForm()
    {
        _playlistService = new PlaylistService(_storage);
        _recentService = new RecentChannelService(_storage);
        _lastService = new LastChannelService(_storage);
        _favoriteService = new FavoriteChannelService(_storage);

        Text = $"AndyTV vNext {Application.ProductVersion.Split('+')[0]}";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        BackColor = Color.Black;

        for (var i = 0; i < _recentItems.Length; i++)
        {
            _recentItems[i] = new ToolStripMenuItem { Visible = false };
            _recentItems[i].Click += OnRecentClick;
        }

        _mediaPlayer = new MediaPlayer(_libVLC)
        {
            EnableMouseInput = false,
            EnableKeyInput = false,
        };

        _healthMonitor = new StreamHealthMonitor(
            isPaused: () => _mediaPlayer.State == VLCState.Paused,
            restart: () =>
            {
                if (_current is { } current)
                {
                    Play(current);
                }
            }
        );
        _healthTimer.Tick += (_, _) => _healthMonitor.Tick();

        _mediaPlayer.Playing += OnPlaying;
        _mediaPlayer.TimeChanged += (_, _) => _healthMonitor.MarkActivity();
        _mediaPlayer.PositionChanged += (_, _) => _healthMonitor.MarkActivity();

        _muteItem.Click += (_, _) =>
        {
            _mediaPlayer.Mute = !_mediaPlayer.Mute;
            _muteItem.Text = _mediaPlayer.Mute ? "Unmute" : "Mute";
        };
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
        // Static parts (header, Manage, recents, favorites) build instantly from local
        // storage so the menu is usable right away; channels stream in afterwards.
        BuildStaticMenu();

        // Play the last channel first — it only needs local storage, so playback
        // starts without waiting on the (networked) playlist refresh below.
        if (_lastService.LoadLastChannel() is { } last)
        {
            Play(last);
        }

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
        using var form = new GuideForm();
        form.ShowDialog(this);
        UpdateCursor();
    }

    private void BuildStaticMenu()
    {
        _menu.Items.Clear();
        _channelItems.Clear();

        var version = Application.ProductVersion.Split('+')[0];
        var header = new ToolStripMenuItem($"AndyTV vNext - {version}");
        header.Click += (_, _) => OpenUrl(UpdateService.RepoUrl);
        _menu.Items.Add(header);
        _menu.Items.Add(new ToolStripSeparator());

        var manage = new ToolStripMenuItem("Manage");
        manage.DropDownItems.Add("Search\u2026", null, (_, _) => SearchChannels());
        manage.DropDownItems.Add("Guide", null, (_, _) => ShowGuide());
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
        manage.DropDownItems.Add(_muteItem);
        manage.DropDownItems.Add("New Window", null, (_, _) => NewWindow());
        manage.DropDownItems.Add(new ToolStripSeparator());
        manage.DropDownItems.Add("Exit", null, (_, _) => Close());
        _menu.Items.Add(manage);
        _menu.Items.Add(new ToolStripSeparator());

        // Recent sits above the favorites list, separated only when both exist.
        _menu.Items.AddRange(_recentItems);
        _menu.Items.Add(_favoritesSeparator);
        _menu.Items.Add(_recentSeparator);
        RebuildFavorites();
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

    // Runs off the UI thread; only creates ToolStripItems (safe until added to the menu).
    private List<ToolStripItem> BuildChannelItems()
    {
        var items = new List<ToolStripItem>();

        foreach (
            var (playlist, channels) in _playlistService.PlaylistChannels.Where(x =>
                x.Playlist.ShowInMenu
            )
        )
        {
            var item = new ToolStripMenuItem(playlist.Name) { Enabled = channels.Count > 0 };
            foreach (var node in ChannelMatcher.BuildPlaylistNodes(playlist, channels))
            {
                item.DropDownItems.Add(Render(node));
            }
            items.Add(item);
        }

        // US/UK match only playlists flagged for it, so TV-show/movie playlists don't
        // pollute the curated lists; 24-7 still spans all channels.
        var usUkChannels = _playlistService
            .PlaylistChannels.Where(x => x.Playlist.ShowInUsUk)
            .SelectMany(x => x.Channels)
            .GroupBy(c => c.Url, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        items.Add(Render(ChannelMatcher.BuildTopRegion("US", ChannelService.TopUs(), usUkChannels)));
        items.Add(Render(ChannelMatcher.BuildTopRegion("UK", ChannelService.TopUk(), usUkChannels)));
        var menu247 = Render(ChannelMatcher.Build247(_playlistService.Channels));
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
            _menu.Items.Insert(index++, FavoriteLeaf(fav));
        }
        foreach (
            var group in _favoriteService
                .Favorites.Where(f => !string.IsNullOrWhiteSpace(f.Group))
                .GroupBy(f => f.Group, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
        )
        {
            var groupItem = new ToolStripMenuItem(group.Key);
            foreach (var fav in group)
            {
                groupItem.DropDownItems.Add(FavoriteLeaf(fav));
            }
            _menu.Items.Insert(index++, groupItem);
        }
        RefreshRecent();
    }

    private ToolStripMenuItem Render(MenuNode node)
    {
        if (node.Channel is { } channel)
        {
            var leaf = new ToolStripMenuItem(node.Text);
            leaf.Click += (_, _) => Play(channel);
            return leaf;
        }

        var item = new ToolStripMenuItem(node.Text);
        if (node.Children is not null)
        {
            foreach (var child in node.Children)
            {
                item.DropDownItems.Add(Render(child));
            }
        }
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

    private ToolStripMenuItem FavoriteLeaf(Channel fav)
    {
        var leaf = new ToolStripMenuItem(fav.DisplayName);
        leaf.Click += (_, _) => Play(fav);
        return leaf;
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
        var recents = _recentService.GetRecentChannels();
        for (var i = 0; i < _recentItems.Length; i++)
        {
            if (i < recents.Count)
            {
                var r = recents[i];
                _recentItems[i].Text = r.DisplayName;
                _recentItems[i].Tag = r;
                _recentItems[i].Visible = true;
            }
            else
            {
                _recentItems[i].Visible = false;
            }
        }
        var hasFavorites = _favoriteService.Favorites.Count > 0;
        _favoritesSeparator.Visible = recents.Count > 0 && hasFavorites;
        // Divider hidden only when nothing sits above it (no recents and no favorites).
        _recentSeparator.Visible = recents.Count > 0 || hasFavorites;
    }

    private void OnRecentClick(object sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem { Tag: Channel r })
        {
            Play(r);
        }
    }

    private void Play(Channel channel)
    {
        _current = channel;
        _pending = channel;
        _healthMonitor.MarkActivity();
        UpdateCursor();
        using var media = new Media(_libVLC, new Uri(channel.Url));
        _mediaPlayer.Play(media);
    }

    private void OnPlaying(object sender, EventArgs e)
    {
        _healthMonitor.MarkActivity();
        if (_pending is not { } played)
        {
            return;
        }
        _pending = null;

        if (InvokeRequired)
        {
            BeginInvoke(() => CommitRecent(played));
        }
        else
        {
            CommitRecent(played);
        }
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts.Cancel();
            _cts.Dispose();
            _healthTimer.Dispose();
            _mediaPlayer.Playing -= OnPlaying;
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }
        base.Dispose(disposing);
    }
}