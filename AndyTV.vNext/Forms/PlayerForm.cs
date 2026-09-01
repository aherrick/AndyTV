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
    // Cursor/menu state: the wait spinner shows while channels are still loading
    // (menu not ready) or a channel is connecting (pending); the menu is suppressed
    // until ready so a huge playlist can't be right-clicked mid-build.
    private bool _menuReady;
    private bool _menuOpen;
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
        _mediaPlayer.EncounteredError += OnPlaybackError;
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

        _menu.Opening += (_, e) =>
        {
            if (!_menuReady)
            {
                e.Cancel = true;
                return;
            }
            _menuOpen = true;
            _muteItem.Text = _mediaPlayer.Mute ? "Unmute" : "Mute";
            _addFavoriteItem.Enabled = _current is { } c && !_favoriteService.IsFavorite(c);
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
        this.EnterFullscreen();
        UpdateCursor();
        _healthTimer.Start();
        await Initialize();
    }

    private async Task Initialize()
    {
        SetBusy(true);

        // Play the last channel first — it only needs local storage, so playback
        // starts without waiting on the (networked) playlist refresh below.
        if (_lastService.LoadLastChannel() is { } last)
        {
            Play(last);
        }

        _playlists = _playlistService.LoadPlaylists();
        await _playlistService.RefreshChannelsAsync();
        RebuildMenu();
        SetBusy(false);

        _ = RunHourlyRefresh();
    }

    private void SetBusy(bool busy)
    {
        _menuReady = !busy;
        UpdateCursor();
    }

    // Single place that decides the cursor: spinner while loading (menu not ready)
    // or connecting (pending); otherwise visible for the open menu, and hidden
    // (fullscreen) or default (windowed) when idle.
    private void UpdateCursor()
    {
        if (!_menuReady || _pending is not null)
        {
            _videoView.ShowWaiting();
        }
        else if (_menuOpen)
        {
            _videoView.ShowDefault();
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
                SetBusy(true);
                await _playlistService.RefreshChannelsAsync();
                RebuildMenu();
                Logger.Info("[REFRESH] Hourly channel refresh complete");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Hourly refresh failed");
            }
            finally
            {
                SetBusy(false);
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
        SetBusy(true);
        await _playlistService.RefreshChannelsAsync();
        RebuildMenu();
        SetBusy(false);
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        var version = Application.ProductVersion.Split('+')[0];
        var header = new ToolStripMenuItem($"AndyTV vNext - {version}");
        header.Click += (_, _) => OpenUrl(UpdateService.RepoUrl);
        _menu.Items.Add(header);
        _menu.Items.Add(new ToolStripSeparator());

        var manage = new ToolStripMenuItem("Manage");
        manage.DropDownItems.Add("Playlists\u2026", null, async (_, _) => await ManagePlaylists());
        manage.DropDownItems.Add(new ToolStripSeparator());
        manage.DropDownItems.Add(_addFavoriteItem);
        manage.DropDownItems.Add("Favorites\u2026", null, (_, _) => ManageFavorites());
        manage.DropDownItems.Add(new ToolStripSeparator());
        manage.DropDownItems.Add(
            "Check for Updates",
            null,
            async (_, _) => await UpdateService.Check()
        );
        manage.DropDownItems.Add(_muteItem);
        manage.DropDownItems.Add(new ToolStripSeparator());
        manage.DropDownItems.Add("Exit", null, (_, _) => Close());
        _menu.Items.Add(manage);
        _menu.Items.Add(new ToolStripSeparator());

        // Recent sits above the favorites list, separated only when both exist.
        _menu.Items.AddRange(_recentItems);
        _menu.Items.Add(_favoritesSeparator);
        foreach (
            var fav in _favoriteService.Favorites.Where(f => string.IsNullOrWhiteSpace(f.Group))
        )
        {
            _menu.Items.Add(FavoriteLeaf(fav));
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
            _menu.Items.Add(groupItem);
        }
        _menu.Items.Add(_recentSeparator);
        RefreshRecent();

        foreach (var (playlist, channels) in _playlistService.PlaylistChannels.Where(x => x.Playlist.ShowInMenu))
        {
            var item = new ToolStripMenuItem(playlist.Name) { Enabled = channels.Count > 0 };
            foreach (var node in ChannelMatcher.BuildPlaylistNodes(playlist, channels))
            {
                item.DropDownItems.Add(Render(node));
            }
            _menu.Items.Add(item);
        }

        var topChannels = _playlistService.Channels;
        _menu.Items.Add(
            Render(ChannelMatcher.BuildTopRegion("US", ChannelService.TopUs(), topChannels))
        );
        _menu.Items.Add(
            Render(ChannelMatcher.BuildTopRegion("UK", ChannelService.TopUk(), topChannels))
        );
        var menu247 = Render(ChannelMatcher.Build247(topChannels));
        if (menu247.DropDownItems.Count > 0)
        {
            _menu.Items.Add(menu247);
        }
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
            RebuildMenu();
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
        RebuildMenu();
    }

    private ToolStripMenuItem FavoriteLeaf(Channel fav)
    {
        var leaf = new ToolStripMenuItem(fav.DisplayName);
        leaf.Click += (_, _) => Play(fav);
        return leaf;
    }

    private static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

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

    // On real failure clear the pending state so the spinner resolves; success
    // clears it in OnPlaying.
    private void OnPlaybackError(object sender, EventArgs e)
    {
        _pending = null;
        UpdateCursor();
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

    // VLC renders this directly on the video frame and auto-hides after Timeout.
    private void ShowNowPlaying(string text)
    {
        _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Enable, 1);
        _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Size, 28);
        _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Color, 0xFF0000);
        _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Opacity, 255);
        _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Position, 10);
        _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Timeout, 2500);
        _mediaPlayer.SetMarqueeString(VideoMarqueeOption.Text, text);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts.Cancel();
            _cts.Dispose();
            _healthTimer.Dispose();
            _mediaPlayer.Playing -= OnPlaying;
            _mediaPlayer.EncounteredError -= OnPlaybackError;
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }
        base.Dispose(disposing);
    }
}