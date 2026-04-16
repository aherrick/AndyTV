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

    private const int HealthCheckMilliseconds = 1000;

    private Media _currentMedia;
    private bool _disposed;

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
                if (_disposed || string.IsNullOrEmpty(_viewModel.Url))
                {
                    return;
                }

                Dispatcher.Dispatch(() =>
                {
                    if (!_disposed)
                    {
                        Play(_viewModel.Url);
                    }
                });
            }
        );

        _mediaPlayer.TimeChanged += (_, _) => _healthMonitor.MarkActivity();
        _mediaPlayer.PositionChanged += (_, _) => _healthMonitor.MarkActivity();
        _mediaPlayer.Playing += (_, _) => _healthMonitor.MarkActivity();

        _healthTimer = Dispatcher.CreateTimer();
        _healthTimer.Interval = TimeSpan.FromMilliseconds(HealthCheckMilliseconds);
        _healthTimer.Tick += OnHealthTimerTick;

        App.AppResumed += OnAppResumed;

        Play(url);
        _healthTimer.Start();
    }

    private void OnAppResumed(object sender, EventArgs e)
    {
        if (_disposed || string.IsNullOrEmpty(_viewModel.Url))
        {
            return;
        }

        // Re-attach the native surface in case it went stale during suspension.
        VideoView.MediaPlayer = _mediaPlayer;

        // Only force-restart if VLC isn't already actively playing/buffering.
        // If it is healthy, the existing health monitor will catch any stall within ~4s on its own.
        var state = _mediaPlayer.State;
        if (state != VLCState.Playing && state != VLCState.Buffering && state != VLCState.Opening)
        {
            Play(_viewModel.Url);
        }

        if (!_healthTimer.IsRunning)
        {
            _healthTimer.Start();
        }
    }

    private void Play(string url)
    {
        _healthMonitor.MarkActivity();
        _mediaPlayer.Stop();

        var previous = _currentMedia;
        _currentMedia = new Media(_libVLC, new Uri(url));
        _mediaPlayer.Play(_currentMedia);
        previous?.Dispose();
    }

    private void OnHealthTimerTick(object sender, EventArgs e)
    {
        if (_disposed || string.IsNullOrEmpty(_viewModel.Url))
        {
            return;
        }

        _healthMonitor.Tick();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _disposed = true;

        DeviceDisplay.Current.KeepScreenOn = false;
        App.AppResumed -= OnAppResumed;

        _healthTimer.Stop();
        _mediaPlayer.Stop();
        VideoView.MediaPlayer = null;

        _currentMedia?.Dispose();
        _mediaPlayer.Dispose();
        _libVLC.Dispose();
    }
}