using AndyTV.Helpers;
using AndyTV.Helpers.Menu;
using AndyTV.Models;
using AndyTV.Services;
using AndyTV.UI;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace AndyTV
{
    public partial class Form1 : Form
    {
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;
        private readonly NotificationService _notificationService;
        private readonly RecentChannelsService _recentChannelsService;
        private readonly UpdateService _updateService;

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

        public Form1(
            LibVLC libVLC,
            MediaPlayer mediaPlayer,
            RecentChannelsService recentChannelsService,
            UpdateService updateService
        )
        {
            _libVLC = libVLC;
            _mediaPlayer = mediaPlayer;
            _recentChannelsService = recentChannelsService;
            _updateService = updateService;

            InitializeComponent();

            // Create NotificationService after form is initialized
            _notificationService = new NotificationService(this);

            Logger.Info("Starting AndyTV...");

            MaximizeWindow();

            Icon = new Icon("AndyTV.ico");

            _mediaPlayer.Stopped += delegate
            {
                Logger.Info("[VLC] Stopped");

                Play(_currentChannelName, _mediaPlayer.Media.Mrl);
            };
            _mediaPlayer.EndReached += delegate
            {
                Logger.Warn("[VLC] EndReached");

                Play(_currentChannelName, _mediaPlayer.Media.Mrl);
            };
            _mediaPlayer.EncounteredError += delegate
            {
                Logger.Error("[VLC] EncounteredError");

                Play(_currentChannelName, _mediaPlayer.Media.Mrl);
            };

            _mediaPlayer.Playing += delegate
            {
                Logger.Error("[VLC] EncounteredError");

                CursorHelper.Hide();
                _notificationService.ShowToast(_currentChannelName);

                _recentChannelsService.AddOrPromote(
                    new Channel("Recent", _currentChannelName, _mediaPlayer.Media.Mrl)
                );
            };

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
            ResizeEnd += delegate
            {
                if (WindowState == FormWindowState.Normal)
                {
                    _manuallyAdjustedBounds = Bounds;
                }
            };

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

            var last = ChannelDataService.LoadLastChannel();
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
                _updateService,
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

            _menuRecentChannelHelper = new MenuRecentChannelHelper(
                _contextMenuStrip,
                ChItem_Click,
                _recentChannelsService
            );
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
            CursorHelper.ShowWaiting();

            await _menuTVChannelHelper.LoadChannels(ChItem_Click, source.Url);

            CursorHelper.Hide();

            Logger.Info("[CHANNELS] Loaded");
        }

        private void ChItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag != null)
            {
                Play(item.Text, item.Tag.ToString());
            }
        }

        private void Play(string channel, string url)
        {
            Logger.Info($"[PLAY][BEGIN] channel='{channel}' url='{url}'");

            _currentChannelName = channel;
            CursorHelper.ShowWaiting();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using var media = new Media(_libVLC, url, FromType.FromLocation);
                    _mediaPlayer.Play(media);
                }
                catch (Exception ex)
                {
                    Logger.Error($"[PLAY][ERROR] channel='{channel}' url='{url}' ex={ex}");
                    CursorHelper.ShowDefault();
                }
            });
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
                var prevChannel = _recentChannelsService.GetPrevious();
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
                ChannelDataService.SaveLastChannel(_currentChannelName, _mediaPlayer.Media.Mrl);
            }
        }
    }
}