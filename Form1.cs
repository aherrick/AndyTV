using AndyTV.Helpers;
using AndyTV.Helpers.Menu;
using AndyTV.Models;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace AndyTV;

public partial class Form1 : Form
{
    private readonly LibVLC _libVLC = new();
    private readonly MediaPlayer _mediaPlayer;
    private readonly CursorHelper _cursorHelper;
    private readonly ToastHelper _toastHelper;

    // menu
    private readonly ContextMenuStrip _contextMenuStrip = new();

    private MenuRecentChannelHelper _menuRecentChannelHelper;
    private MenuSettingsHelper _menuSettingsHelper;
    private MenuTVChannelHelper _menuTVChannelHelper;

    private string _currentChannelName = "";

    public Form1()
    {
        InitializeComponent();

        MaximizeWindow();

        Logger.Info("Starting AndyTV...");

        // Load icon from file
        Icon = new Icon("AndyTV.ico");

        BackColor = Color.Black;

        _cursorHelper = new CursorHelper(this);

        _toastHelper = new ToastHelper(this);

        _mediaPlayer = new MediaPlayer(_libVLC)
        {
            EnableHardwareDecoding = true,
            EnableKeyInput = false,
            EnableMouseInput = false,
        };

        _mediaPlayer.Playing += MediaPlayer_Playing;

        _mediaPlayer.EncounteredError += (_, __) =>
            _ = Play(_currentChannelName, _mediaPlayer.Media.Mrl); // attempt restart
        _mediaPlayer.EndReached += (_, __) => _ = Play(_currentChannelName, _mediaPlayer.Media.Mrl); // attempt restart

        var videoView = new VideoView
        {
            Dock = DockStyle.Fill,
            MediaPlayer = _mediaPlayer,
            ContextMenuStrip = _contextMenuStrip,
        };

        videoView.MouseDoubleClick += VideoView_MouseDoubleClick;
        videoView.MouseUp += VideoView_MouseUp;
        videoView.MouseDown += VideoView_MouseDown;

        Controls.Add(videoView);

        // form events
        ResizeEnd += AndyTV_ResizeEnd;
        Shown += AndyTV_Shown;
        FormClosing += AndyTV_FormClosing;

        _contextMenuStrip.Opening += (s, e) =>
        {
            _cursorHelper.ShowDefault();
        };

        _contextMenuStrip.Closing += (s, e) =>
        {
            _cursorHelper.Hide();
        };
    }

    #region UI Helpers

    private void AndyTV_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (_mediaPlayer?.Media?.Mrl is string mrl && !string.IsNullOrWhiteSpace(mrl))
        {
            LastChannelHelper.Save(_currentChannelName, mrl);
        }
    }

    private async void AndyTV_Shown(object sender, EventArgs e)
    {
        // auto-play immediately on startup
        var last = LastChannelHelper.Load();
        if (last is not null)
        {
            _ = Play(last.Value.Name, last.Value.Url);
        }

        var appVersionName = $"AndyTV v{AppHelper.Version}";
        Text = appVersionName;

        _menuSettingsHelper = new MenuSettingsHelper(
            menu: _contextMenuStrip,
            appVersionName: appVersionName,
            mediaPlayer: _mediaPlayer
        );

        _menuRecentChannelHelper = new MenuRecentChannelHelper(_contextMenuStrip, ChItem_Click);
        _menuRecentChannelHelper.RebuildRecentMenu();

        _menuTVChannelHelper = new MenuTVChannelHelper(_contextMenuStrip);

        var source = M3USourceStore.TryGetFirst();
        if (source is null)
        {
            RestoreWindow();
            // Prompt user for M3U source if none found
            source = M3USourceStore.PromptNewSource();
            if (source is null)
            {
                Close(); // exits the form/app gracefully if cancelled
                return;
            }
        }

        _cursorHelper.ShowWaiting();

        await _menuTVChannelHelper.LoadChannels(channelClick: ChItem_Click, m3uURL: source.Url);

        _cursorHelper.ShowDefault();
    }

    private async void ChItem_Click(object sender, EventArgs e)
    {
        var item = (ToolStripMenuItem)sender;

        await Play(item.Text, item.Tag.ToString());
    }

    private async Task Play(string channel, string url)
    {
        await Task.Yield(); // keep UI responsive

        _mediaPlayer.Stop();

        _currentChannelName = channel;
        _cursorHelper.ShowWaiting();

        using var media = new Media(_libVLC, url, FromType.FromLocation);
        _mediaPlayer.Play(media);
    }

    private void AndyTV_ResizeEnd(object sender, EventArgs e)
    {
        // Update the last manual size only if the window is in normal state
        if (WindowState == FormWindowState.Normal)
        {
            _manuallyAdjustedBounds = Bounds;
        }
    }

    private void MaximizeWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        Bounds = Screen.FromControl(this).Bounds;

        _cursorHelper?.Hide();
    }

    // Class-level variable to store the manually adjusted bounds
    private Rectangle? _manuallyAdjustedBounds;

    private void RestoreWindow()
    {
        FormBorderStyle = FormBorderStyle.Sizable;
        WindowState = FormWindowState.Normal;
        Bounds = _manuallyAdjustedBounds ?? Screen.FromControl(this).WorkingArea;

        _cursorHelper.ShowDefault();
    }

    #endregion UI Helpers

    #region VideoView Events

    private DateTime? _mouseDownLeftPrevChannel;

    private void VideoView_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _mouseDownLeftPrevChannel = DateTime.Now;
        }
    }

    private void VideoView_MouseUp(object sender, MouseEventArgs e)
    {
        if (
            e.Button == MouseButtons.Left
            && _mouseDownLeftPrevChannel != null
            && _mouseDownLeftPrevChannel.Value.AddSeconds(1) < DateTime.Now
        )
        {
            var prevChannel = _menuRecentChannelHelper.GetPrevious();
            if (prevChannel != null)
            {
                _ = Play(prevChannel.Name, prevChannel.Url);
                _currentChannelName = prevChannel.Name;
            }
        }

        if (e.Button == MouseButtons.Middle)
        {
            _mediaPlayer.Mute = !_mediaPlayer.Mute;
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

    #endregion VideoView Events

    #region MediaPlayer Events

    private void MediaPlayer_Playing(object sender, EventArgs e)
    {
        _cursorHelper.Hide();

        _toastHelper.Show(_currentChannelName);

        _menuRecentChannelHelper.AddOrPromote(
            new Channel(Group: "Recent", Name: _currentChannelName, Url: _mediaPlayer.Media.Mrl)
        );
    }

    #endregion MediaPlayer Events
}