using AndyTV.Data.Models;
using AndyTV.Data.Services;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using Microsoft.VisualBasic;

namespace AndyTV.vNext;

sealed class PlayerForm : Form
{
    private readonly LibVLC _libVLC = new();
    private readonly MediaPlayer _mediaPlayer;
    private readonly VideoView _videoView;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem[] _recentItems = new ToolStripMenuItem[5];
    private readonly ToolStripSeparator _recentSeparator = new();

    private readonly IStorageProvider _storage = new Storage();
    private readonly PlaylistService _playlistService;
    private readonly RecentChannelService _recentService;
    private readonly LastChannelService _lastService;
    private List<Playlist> _playlists = [];

    private Channel _pending;
    private FormWindowState _restoreState = FormWindowState.Maximized;
    private Rectangle _restoreBounds;

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
                if (_pending is { } current)
                {
                    Play(current);
                }
            });
        _healthTimer.Tick += (_, _) => _healthMonitor.Tick();

        _mediaPlayer.Playing += OnPlaying;
        _mediaPlayer.EncounteredError += OnLoadStopped;
        _mediaPlayer.Stopped += OnLoadStopped;
        _mediaPlayer.TimeChanged += (_, _) => _healthMonitor.MarkActivity();
        _mediaPlayer.PositionChanged += (_, _) => _healthMonitor.MarkActivity();

        var videoView = new VideoView
        {
            Dock = DockStyle.Fill,
            MediaPlayer = _mediaPlayer,
            BackColor = Color.Black,
            ContextMenuStrip = _menu
        };
        videoView.MouseDoubleClick += (_, _) =>
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
        };
        _videoView = videoView;
        Controls.Add(videoView);

        _menu.Opening += (_, _) => _videoView.ShowDefault();
        _menu.Closed += (_, _) => _videoView.SetCursorForCurrentView();
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };

        Shown += async (_, _) =>
        {
            this.EnterFullscreen();
            _videoView.SetCursorForCurrentView();
            _healthTimer.Start();
            await Initialize();
        };
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
    }

    private async Task AddPlaylist()
    {
        var name = Interaction.InputBox("Playlist name", "Add Playlist");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }
        var url = Interaction.InputBox("M3U URL or file path", "Add Playlist");
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        _playlists.Add(new Playlist { Name = name, Url = url, ShowInMenu = true });
        _playlistService.SavePlaylists(_playlists);
        await _playlistService.RefreshChannelsAsync();
        RebuildMenu();
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

        _menu.Items.Add(BuildTopMenu());
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add("Add Playlist\u2026", null, async (_, _) => await AddPlaylist());
        if (_playlists.Count > 0)
        {
            _menu.Items.Add("Manage Playlists\u2026", null, async (_, _) => await ManagePlaylists());
        }

        var visible = _playlistService.PlaylistChannels.Where(x => x.Playlist.ShowInMenu).ToList();
        if (visible.Count > 0)
        {
            _menu.Items.Add(new ToolStripSeparator());
        }
        foreach (var (playlist, channels) in visible)
        {
            var item = new ToolStripMenuItem(playlist.Name) { Enabled = channels.Count > 0 };
            if (playlist.GroupByFirstChar)
            {
                foreach (var group in ChannelMatcher.GroupByFirst(channels))
                {
                    var groupItem = new ToolStripMenuItem(group.Key);
                    foreach (var channel in group)
                    {
                        AddChannel(groupItem.DropDownItems, channel);
                    }
                    item.DropDownItems.Add(groupItem);
                }
            }
            else
            {
                foreach (var channel in channels)
                {
                    AddChannel(item.DropDownItems, channel);
                }
            }
            _menu.Items.Add(item);
        }
    }

    private void AddChannel(ToolStripItemCollection items, Channel channel) =>
        items.Add(channel.DisplayName, null, (_, _) => Play(channel));

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

    private ToolStripMenuItem BuildTopMenu()
    {
        var lookup = ChannelMatcher.BuildLookup(_playlistService.PlaylistChannels);
        var top = new ToolStripMenuItem("Top");
        top.DropDownItems.Add(BuildRegionMenu("US", ChannelService.TopUs(), lookup));
        top.DropDownItems.Add(BuildRegionMenu("UK", ChannelService.TopUk(), lookup));
        return top;
    }

    private ToolStripMenuItem BuildRegionMenu(
        string region,
        Dictionary<string, List<ChannelTop>> categories,
        Dictionary<string, List<Channel>> lookup)
    {
        var regionItem = new ToolStripMenuItem(region);
        foreach (var (category, channels) in categories)
        {
            var categoryItem = new ToolStripMenuItem(category);
            foreach (var channel in channels)
            {
                var matches = ChannelMatcher.Match(channel, lookup);
                var item = new ToolStripMenuItem(channel.Name) { Enabled = matches.Count > 0 };
                if (matches.Count == 1)
                {
                    item.Click += (_, _) => Play(matches[0]);
                }
                else
                {
                    foreach (var ch in matches)
                    {
                        item.DropDownItems.Add(ch.DisplayName, null, (_, _) => Play(ch));
                    }
                }
                categoryItem.DropDownItems.Add(item);
            }
            regionItem.DropDownItems.Add(categoryItem);
        }
        return regionItem;
    }

    private void Play(Channel channel)
    {
        _pending = channel;
        _healthMonitor.MarkActivity();
        _videoView.ShowWaiting();
        using var media = new Media(_libVLC, new Uri(channel.Url));
        _mediaPlayer.Play(media);
    }

    private void OnLoadStopped(object sender, EventArgs e) =>
        _videoView.SetCursorForCurrentView();

    private void OnPlaying(object sender, EventArgs e)
    {
        _healthMonitor.MarkActivity();
        if (_pending is not { } played)
        {
            return;
        }

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
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _healthTimer.Dispose();
            _mediaPlayer.Playing -= OnPlaying;
            _mediaPlayer.EncounteredError -= OnLoadStopped;
            _mediaPlayer.Stopped -= OnLoadStopped;
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }
        base.Dispose(disposing);
    }
}
