using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using Microsoft.VisualBasic;

namespace AndyTV.vNext;

sealed class PlayerForm : Form
{
    private readonly LibVLC _libVLC = new();
    private readonly MediaPlayer _mediaPlayer;
    private readonly ContextMenuStrip _menu = new();
    private readonly List<(PlaylistRef Ref, List<ChannelRef> Channels)> _loaded = [];
    private readonly ToolStripMenuItem[] _recentItems = new ToolStripMenuItem[5];
    private readonly ToolStripSeparator _recentSeparator = new();
    private AppState _state = new();
    private ChannelRef? _pending;
    private bool _cursorHidden;

    public PlayerForm()
    {
        Text = "AndyTV vNext";
        BackColor = Color.Black;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;

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
        _mediaPlayer.Playing += OnPlaying;
        _mediaPlayer.EncounteredError += OnLoadStopped;
        _mediaPlayer.Stopped += OnLoadStopped;

        var videoView = new VideoView
        {
            Dock = DockStyle.Fill,
            MediaPlayer = _mediaPlayer,
            BackColor = Color.Black,
            ContextMenuStrip = _menu
        };
        videoView.MouseDoubleClick += (_, _) => ToggleWindowState();
        Controls.Add(videoView);

        _menu.Opening += (_, _) => SetCursorHidden(false);
        _menu.Closed += (_, _) => SetCursorHidden(IsFullscreen);
        SetCursorHidden(true);
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        Shown += async (_, _) => await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _state = StateService.Load();
        foreach (var playlist in _state.Playlists)
            await TryLoadAsync(playlist);
        RebuildMenu();

        if (_state.Last is { } last)
            Play(last);
    }

    private async Task AddPlaylistAsync()
    {
        var name = Interaction.InputBox("Playlist name", "Add Playlist");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var source = Interaction.InputBox("M3U URL or file path", "Add Playlist");
        if (string.IsNullOrWhiteSpace(source))
            return;

        var playlist = new PlaylistRef { Name = name, Source = source };
        if (await TryLoadAsync(playlist))
        {
            _state.Playlists.Add(playlist);
            StateService.Save(_state);
            RebuildMenu();
        }
    }

    private async Task<bool> TryLoadAsync(PlaylistRef playlist)
    {
        try
        {
            _loaded.Add((playlist, await PlaylistService.LoadAsync(playlist)));
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Failed to load playlist", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void ManagePlaylists()
    {
        using var form = new PlaylistManagerForm(_state.Playlists);
        form.ShowDialog(this);
        _loaded.RemoveAll(l => !_state.Playlists.Contains(l.Ref));
        StateService.Save(_state);
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        _menu.Items.Clear();

        foreach (var item in _recentItems)
            _menu.Items.Add(item);
        _menu.Items.Add(_recentSeparator);
        RefreshRecent();

        _menu.Items.Add(BuildTopMenu());
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add("Add Playlist\u2026", null, async (_, _) => await AddPlaylistAsync());
        if (_state.Playlists.Count > 0)
            _menu.Items.Add("Manage Playlists\u2026", null, (_, _) => ManagePlaylists());

        var visible = _loaded.Where(l => !l.Ref.Hidden).ToList();
        if (visible.Count > 0)
            _menu.Items.Add(new ToolStripSeparator());
        foreach (var (playlist, channels) in visible)
        {
            var item = new ToolStripMenuItem(playlist.Name) { Enabled = channels.Count > 0 };
            if (playlist.Grouped)
            {
                foreach (var group in ChannelService.GroupByFirst(channels))
                {
                    var groupItem = new ToolStripMenuItem(group.Key);
                    foreach (var channel in group)
                        AddChannel(groupItem.DropDownItems, channel);
                    item.DropDownItems.Add(groupItem);
                }
            }
            else
            {
                foreach (var channel in channels)
                    AddChannel(item.DropDownItems, channel);
            }
            _menu.Items.Add(item);
        }
    }

    private void AddChannel(ToolStripItemCollection items, ChannelRef channel) =>
        items.Add(channel.Name, null, (_, _) => Play(channel));

    private void RefreshRecent()
    {
        for (var i = 0; i < _recentItems.Length; i++)
        {
            if (i < _state.Recent.Count)
            {
                var r = _state.Recent[i];
                _recentItems[i].Text = r.Name;
                _recentItems[i].Tag = r;
                _recentItems[i].Visible = true;
            }
            else
            {
                _recentItems[i].Visible = false;
            }
        }
        _recentSeparator.Visible = _state.Recent.Count > 0;
    }

    private void OnRecentClick(object? sender, EventArgs e)
    {
        if (sender is ToolStripMenuItem { Tag: ChannelRef r })
            Play(r);
    }

    private ToolStripMenuItem BuildTopMenu()
    {
        var lookup = ChannelService.BuildLookup(_loaded);
        var top = new ToolStripMenuItem("Top");
        top.DropDownItems.Add(BuildRegionMenu("US", ChannelService.TopUs, lookup));
        top.DropDownItems.Add(BuildRegionMenu("UK", ChannelService.TopUk, lookup));
        return top;
    }

    private ToolStripMenuItem BuildRegionMenu(
        string region,
        Dictionary<string, List<ChannelTop>> categories,
        Dictionary<string, List<ChannelRef>> lookup)
    {
        var regionItem = new ToolStripMenuItem(region);
        foreach (var (category, channels) in categories)
        {
            var categoryItem = new ToolStripMenuItem(category);
            foreach (var channel in channels)
            {
                var matches = ChannelService.Match(channel, lookup);
                var item = new ToolStripMenuItem(channel.Name) { Enabled = matches.Count > 0 };
                if (matches.Count == 1)
                    item.Click += (_, _) => Play(new ChannelRef { Name = channel.Name, Url = matches[0].Url });
                else
                    foreach (var ch in matches)
                        item.DropDownItems.Add(ch.Name, null, (_, _) => Play(new ChannelRef { Name = channel.Name, Url = ch.Url }));
                categoryItem.DropDownItems.Add(item);
            }
            regionItem.DropDownItems.Add(categoryItem);
        }
        return regionItem;
    }

    private void Play(ChannelRef channel)
    {
        _pending = channel;
        UseWaitCursor = true;
        using var media = new Media(_libVLC, new Uri(channel.Url));
        _mediaPlayer.Play(media);
    }

    private void OnLoadStopped(object? sender, EventArgs e)
    {
        if (InvokeRequired)
            BeginInvoke(() => UseWaitCursor = false);
        else
            UseWaitCursor = false;
    }

    private void OnPlaying(object? sender, EventArgs e)
    {
        if (_pending is not { } played)
            return;

        if (InvokeRequired)
            BeginInvoke(() => CommitRecent(played));
        else
            CommitRecent(played);
    }

    private void CommitRecent(ChannelRef played)
    {
        _state.Recent.RemoveAll(r => r.Url == played.Url);
        _state.Recent.Insert(0, played);
        if (_state.Recent.Count > 5)
            _state.Recent.RemoveRange(5, _state.Recent.Count - 5);
        _state.Last = played;
        StateService.Save(_state);
        UseWaitCursor = false;
        RefreshRecent();
    }

    private bool IsFullscreen => FormBorderStyle == FormBorderStyle.None;

    private void SetCursorHidden(bool hide)
    {
        if (hide == _cursorHidden)
            return;
        if (hide)
            Cursor.Hide();
        else
            Cursor.Show();
        _cursorHidden = hide;
    }

    private void ToggleWindowState()
    {
        if (FormBorderStyle == FormBorderStyle.None)
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            SetCursorHidden(false);
        }
        else
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            SetCursorHidden(true);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SetCursorHidden(false);
            _mediaPlayer.Playing -= OnPlaying;
            _mediaPlayer.EncounteredError -= OnLoadStopped;
            _mediaPlayer.Stopped -= OnLoadStopped;
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }
        base.Dispose(disposing);
    }
}
