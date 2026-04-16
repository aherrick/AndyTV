using AndyTV.Data.Services;
using AndyTV.Maui.ViewModels;
using LibVLCSharp.Shared;

namespace AndyTV.Maui.Views;

public partial class PlayerPage : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    private readonly LibVLC _libVLC;
    private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
    private readonly IDispatcherTimer _healthTimer;
    private readonly StreamHealthMonitor _healthMonitor;
    private Window _subscribedWindow;
    private readonly EventHandler _onWindowResumed;

    private const int HealthCheckMilliseconds = 1000;

    public PlayerPage(string url, string channelName)
    {
        InitializeComponent();

        _viewModel = new PlayerViewModel { Url = url, ChannelName = channelName };
        BindingContext = _viewModel;

        DeviceDisplay.Current.KeepScreenOn = true;

        _libVLC = new LibVLC();
        _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
        VideoView.MediaPlayer = _mediaPlayer;

        _healthMonitor = new StreamHealthMonitor(
            isPaused: () => _mediaPlayer.State == VLCState.Paused,
            restart: () =>
            {
                if (string.IsNullOrEmpty(_viewModel.Url))
                    return;

                Play(_viewModel.Url);
            }
        );

        _mediaPlayer.TimeChanged += (_, __) => _healthMonitor.MarkActivity();
        _mediaPlayer.PositionChanged += (_, __) => _healthMonitor.MarkActivity();
        _mediaPlayer.Playing += (_, __) => _healthMonitor.MarkActivity();

        _healthTimer = Dispatcher.CreateTimer();
        _healthTimer.Interval = TimeSpan.FromMilliseconds(HealthCheckMilliseconds);
        _healthTimer.Tick += OnHealthTimerTick;

        _onWindowResumed = (_, _) =>
        {
            if (!string.IsNullOrEmpty(_viewModel.Url))
                Play(_viewModel.Url);
        };

        Play(url);
        _healthTimer.Start();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to window resume to restart playback if iOS backgrounded us.
        // Cache the specific window we bound to so we don't leak it later.
        _subscribedWindow = this.Window ?? Application.Current?.Windows.FirstOrDefault();
        _subscribedWindow?.Resumed += _onWindowResumed;
    }

    private void Play(string url)
    {
        _healthMonitor.MarkActivity();
        _mediaPlayer.Stop();
        _mediaPlayer.Play(new Media(_libVLC, new Uri(url)));
    }

    private void OnHealthTimerTick(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel.Url))
            return;

        _healthMonitor.Tick();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.KeepScreenOn = false;

        if (_subscribedWindow != null)
        {
            _subscribedWindow.Resumed -= _onWindowResumed;
            _subscribedWindow = null;
        }

        _healthTimer.Stop();
        _mediaPlayer.Stop();
        VideoView.MediaPlayer = null;
    }
}