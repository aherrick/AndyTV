using System.Diagnostics;
using AndyTV.Helpers;
using AndyTV.Helpers.Menu;
using AndyTV.Models;
using AndyTV.UI;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace AndyTV
{
    public partial class Form1 : Form
    {
        private readonly LibVLC _libVLC = new();
        private readonly MediaPlayer _mediaPlayer;
        private readonly ToastHelper _toastHelper;

        // Menu
        private readonly ContextMenuStrip _contextMenuStrip = new();

        private MenuRecentChannelHelper _menuRecentChannelHelper;
        private MenuSettingsHelper _menuSettingsHelper;
        private MenuTVChannelHelper _menuTVChannelHelper;
        private MenuFavoriteChannelHelper _menuFavoriteChannelHelper;

        private string _currentChannelName = "";
        private Rectangle _manuallyAdjustedBounds = Rectangle.Empty;

        private DateTime _mouseDownLeftPrevChannel = DateTime.MinValue;
        private DateTime _mouseDownRightExit = DateTime.MinValue;

        public Form1()
        {
            InitializeComponent();

            Logger.Info("Starting AndyTV...");

            MaximizeWindow();

            // Load icon from file

            Icon = new Icon("AndyTV.ico");

            BackColor = Color.Black;

            _toastHelper = new ToastHelper(this);

            _mediaPlayer = new MediaPlayer(_libVLC)
            {
                EnableHardwareDecoding = true,
                EnableKeyInput = false,
                EnableMouseInput = false,
            };

            // VLC state logs
            _mediaPlayer.Playing += delegate
            {
                Logger.Info("[VLC] Playing");
            };
            _mediaPlayer.Stopped += delegate
            {
                Logger.Info("[VLC] Stopped");
            };
            _mediaPlayer.EndReached += delegate
            {
                Logger.Warn("[VLC] EndReached");
                if (
                    _mediaPlayer.Media != null
                    && !string.IsNullOrWhiteSpace(_mediaPlayer.Media.Mrl)
                )
                {
                    Play(_currentChannelName, _mediaPlayer.Media.Mrl);
                }
            };
            _mediaPlayer.EncounteredError += delegate
            {
                Logger.Error("[VLC] EncounteredError");
                if (
                    _mediaPlayer.Media != null
                    && !string.IsNullOrWhiteSpace(_mediaPlayer.Media.Mrl)
                )
                {
                    Play(_currentChannelName, _mediaPlayer.Media.Mrl);
                }
            };

            _mediaPlayer.Playing += MediaPlayer_Playing;

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

            // Form events
            ResizeEnd += AndyTV_ResizeEnd;
            Shown += AndyTV_Shown;
            FormClosing += AndyTV_FormClosing;

            _contextMenuStrip.Opening += delegate
            {
                CursorHelper.ShowDefault();
            };

            _contextMenuStrip.Closing += delegate
            {
                CursorHelper.Hide();
            };
        }

        // --- UI Helpers ---

        private void AndyTV_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.Info("[APP] FormClosing");
            SaveCurrentChannelState();
        }

        private async void AndyTV_Shown(object sender, EventArgs e)
        {
            Logger.Info("[APP] Form Shown, initializing...");

            var last = LastChannelHelper.Load();
            if (last != null)
            {
                Play(last.Value.Name, last.Value.Url);
            }

            var appVersionName = "AndyTV v" + AppHelper.Version;
            Text = appVersionName;

            _menuTVChannelHelper = new MenuTVChannelHelper(_contextMenuStrip);

            _menuSettingsHelper = new MenuSettingsHelper(
                _contextMenuStrip,
                appVersionName,
                _mediaPlayer,
                delegate
                {
                    return new FavoriteChannelForm(_menuTVChannelHelper.Channels);
                },
                delegate
                {
                    _menuFavoriteChannelHelper.RebuildFavoritesMenu();
                },
                SaveCurrentChannelState
            );

            _menuRecentChannelHelper = new MenuRecentChannelHelper(_contextMenuStrip, ChItem_Click);
            _menuRecentChannelHelper.RebuildRecentMenu();

            _menuFavoriteChannelHelper = new MenuFavoriteChannelHelper(
                _contextMenuStrip,
                ChItem_Click
            );
            _menuFavoriteChannelHelper.RebuildFavoritesMenu();

            var source = M3USourceStore.TryGetFirst();
            if (source == null)
            {
                RestoreWindow();
                source = M3USourceStore.PromptNewSource();
                if (source == null)
                {
                    Logger.Warn("[APP] No M3U source selected. Exiting.");
                    Close();
                    return;
                }
            }

            Logger.Info("[CHANNELS] Loading from M3U...");
            CursorHelper.ShowWaiting();

            await _menuTVChannelHelper.LoadChannels(ChItem_Click, source.Url);

            Logger.Info("[CHANNELS] Loaded");
        }

        private void ChItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag != null)
            {
                Play(item.Text, item.Tag.ToString());
            }
        }

        private async void Play(string channel, string url)
        {
            var sw = Stopwatch.StartNew();
            Logger.Info("[PLAY][BEGIN] channel='" + channel + "' url='" + url + "'");

            try
            {
                _mediaPlayer.Stop();
                _currentChannelName = channel;
                CursorHelper.ShowWaiting();

                var media = await Task.Run(() => new Media(_libVLC, url, FromType.FromLocation));
                var started = _mediaPlayer.Play(media);
                Logger.Info("[PLAY] Play() returned=" + started + " channel='" + channel + "'");
            }
            catch (Exception ex)
            {
                Logger.Error("[PLAY][ERROR] channel='" + channel + "' url='" + url + "' ex=" + ex);
                CursorHelper.ShowDefault();
            }
            finally
            {
                sw.Stop();
                Logger.Info(
                    "[PLAY][END] channel='" + channel + "' elapsedMs=" + sw.ElapsedMilliseconds
                );
            }
        }

        private void AndyTV_ResizeEnd(object sender, EventArgs e)
        {
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

            CursorHelper.Hide();
        }

        private void RestoreWindow()
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;

            if (_manuallyAdjustedBounds != Rectangle.Empty)
            {
                Bounds = _manuallyAdjustedBounds;
            }
            else
            {
                Bounds = Screen.FromControl(this).WorkingArea;
            }

            CursorHelper.ShowDefault();
        }

        // --- VideoView Events ---

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
                var prevChannel = _menuRecentChannelHelper.GetPrevious();
                if (prevChannel != null)
                {
                    Play(prevChannel.Name, prevChannel.Url);
                    _currentChannelName = prevChannel.Name;
                }
            }

            if (e.Button == MouseButtons.Middle)
            {
                _mediaPlayer.Mute = !_mediaPlayer.Mute;
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

        // --- MediaPlayer Events ---

        private void SaveCurrentChannelState()
        {
            if (_mediaPlayer.Media != null && !string.IsNullOrWhiteSpace(_mediaPlayer.Media.Mrl))
            {
                LastChannelHelper.Save(_currentChannelName, _mediaPlayer.Media.Mrl);
            }
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            CursorHelper.Hide();
            _toastHelper.Show(_currentChannelName);

            if (_mediaPlayer.Media != null)
            {
                _menuRecentChannelHelper.AddOrPromote(
                    new Channel("Recent", _currentChannelName, _mediaPlayer.Media.Mrl)
                );
            }
        }
    }
}