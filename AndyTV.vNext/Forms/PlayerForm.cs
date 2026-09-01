using AndyTV.Data.Models;
using AndyTV.Data.Services;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace AndyTV.vNext;

sealed class PlayerForm : Form
{
    private readonly LibVLC _libVLC = new();
    private readonly MediaPlayer _mediaPlayer;
    private readonly VideoView _videoView;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem[] _recentItems = new ToolStripMenuItem[5];
    private readonly ToolStripSeparator _recentSeparator = new();

    private readonly IStorageProvider _storage = new LocalStorageProvider();
    private readonly PlaylistService _playlistService;
    private readonly RecentChannelService _recentService;
    private readonly LastChannelService _lastService;
    private List<Playlist> _playlists = [];

    private Channel _current;
    private Channel _pending;
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

        Text = "AndyTV vNext";
        BackColor = Color.Black;

        for (var i = 0; i < _recentItems.Length; i++)
        {
            _recentItems[i] = new ToolStripMenuItem { Visible = false };
            _recentItems[i].Click += OnRecentClick;
        }

        _mediaPlayer = new MediaPlayer(_libVLC)
        {
            EnableMouseInput = false,
            EnableKeyInput = false
        };

        _healthMonitor = new StreamHealthMonitor(
            isPaused: () => _mediaPlayer.State == VLCState.Paused,
            restart: () =>
            {
                if (_current is { } current)
                {
                    Play(current);
                }
            });
        _healthTimer.Tick += (_, _) => _healthMonitor.Tick();

        _mediaPlayer.Playing += OnPlaying;
        _mediaPlayer.EncounteredError += OnPlaybackError;
        _mediaPlayer.TimeChanged += (_, _) => _healthMonitor.MarkActivity();
        _mediaPlayer.PositionChanged += (_, _) => _healthMonitor.MarkActivity();

        _videoView = new VideoView
        {
            Dock = DockStyle.Fill,
            MediaPlayer = _mediaPlayer,
            BackColor = Color.Black,
            ContextMenuStrip = _menu
        };
        // Mouse gestures: double-click = fullscreen, left long-press = previous channel,
        // middle = mute, right long-press = exit, wheel = channel up/down.
        _videoView.MouseDoubleClick += OnVideoDoubleClick;
        _videoView.MouseDown += OnVideoMouseDown;
        _videoView.MouseUp += OnVideoMouseUp;
        _videoView.MouseWheel += OnVideoMouseWheel;
        Controls.Add(_videoView);

        _menu.Opening += (_, _) => _videoView.ShowDefault();
        _menu.Closing += (_, _) => _videoView.SetCursorForCurrentView();
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
        _videoView.SetCursorForCurrentView();
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
        if (e.Button == MouseButtons.Left
            && _leftDown != DateTime.MinValue
            && _leftDown.AddSeconds(LeftHoldSeconds) < DateTime.Now
            && _recentService.GetPrevious() is { } previous)
        {
            Play(previous);
        }
        else if (e.Button == MouseButtons.Middle)
        {
            _mediaPlayer.Mute = !_mediaPlayer.Mute;
        }
        else if (e.Button == MouseButtons.Right
            && _rightDown != DateTime.MinValue
            && _rightDown.AddSeconds(RightHoldSeconds) < DateTime.Now)
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
        _videoView.SetCursorForCurrentView();
        _healthTimer.Start();
        await Initialize();
    }

    private async Task Initialize()
    {
        _playlists = _playlistService.LoadPlaylists();
        await _playlistService.RefreshChannelsAsync();
        RebuildMenu();

        if (_lastService.LoadLastChannel() is { } last)
        {
            Play(last);
        }

        _ = RunHourlyRefresh();
        _ = UpdateService.Check();
    }

    private async Task RunHourlyRefresh()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                try
                {
                    await _playlistService.RefreshChannelsAsync();
                    RebuildMenu();
                    Logger.Info("[REFRESH] Hourly channel refresh complete");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Hourly refresh failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ManagePlaylists()
    {
        using var form = new PlaylistManagerForm(_playlists);
        form.ShowDialog(this);
        _playlistService.SavePlaylists(_playlists);
        await _playlistService.RefreshChannelsAsync();
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        foreach (var item in _recentItems)
        {
            _menu.Items.Add(item);
        }
        _menu.Items.Add(_recentSeparator);
        RefreshRecent();

        var topChannels = _playlistService.Channels;
        _menu.Items.Add(Render(ChannelMatcher.BuildTopRegion("US", ChannelService.TopUs(), topChannels)));
        _menu.Items.Add(Render(ChannelMatcher.BuildTopRegion("UK", ChannelService.TopUk(), topChannels)));
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add("Playlists\u2026", null, async (_, _) => await ManagePlaylists());

        var visible = _playlistService.PlaylistChannels.Where(x => x.Playlist.ShowInMenu).ToList();
        if (visible.Count > 0)
        {
            _menu.Items.Add(new ToolStripSeparator());
        }
        foreach (var (playlist, channels) in visible)
        {
            var item = new ToolStripMenuItem(playlist.Name) { Enabled = channels.Count > 0 };
            foreach (var node in ChannelMatcher.BuildPlaylistNodes(playlist, channels))
            {
                item.DropDownItems.Add(Render(node));
            }
            _menu.Items.Add(item);
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
        _recentSeparator.Visible = recents.Count > 0;
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
        _videoView.ShowWaiting();
        using var media = new Media(_libVLC, new Uri(channel.Url));
        _mediaPlayer.Play(media);
    }

    // Only clears the loading cursor on real failure; success clears it in OnPlaying.
    private void OnPlaybackError(object sender, EventArgs e) =>
        _videoView.SetCursorForCurrentView();

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
        _videoView.SetCursorForCurrentView();
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
