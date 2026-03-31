using AndyTV.Data.Services;
using AndyTV.Maui.ViewModels;
using LibVLCSharp.Shared;

namespace AndyTV.Maui.Views;

public partial class PlayerPage : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    private readonly IDispatcherTimer _healthTimer;
    private readonly StreamHealthMonitor _healthMonitor;
    private LibVLC _libVLC;
    private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;

    private const int HealthCheckMilliseconds = 1000;

    public PlayerPage(string url, string channelName)
    {
        InitializeComponent();

        _viewModel = new PlayerViewModel { Url = url, ChannelName = channelName };
        BindingContext = _viewModel;

        DeviceDisplay.Current.KeepScreenOn = true;

        // Lazy init: create LibVLC only when the player page opens, not at app startup.
        _libVLC = new LibVLC();
        _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
        VideoView.MediaPlayer = _mediaPlayer;

        _healthMonitor = new StreamHealthMonitor(
            isPaused: () => _mediaPlayer.State == VLCState.Paused,
            restart: () =>
            {
                if (string.IsNullOrEmpty(_viewModel.Url))
                {
                    return;
                }

                Play(_viewModel.Url);
            }
        );

        _mediaPlayer.TimeChanged += (_, _) => _healthMonitor.MarkActivity();
        _mediaPlayer.Playing += (_, _) => _healthMonitor.MarkActivity();

        _healthTimer = Dispatcher.CreateTimer();
        _healthTimer.Interval = TimeSpan.FromMilliseconds(HealthCheckMilliseconds);
        _healthTimer.Tick += OnHealthTimerTick;

        Play(url);
        _healthTimer.Start();
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
        {
            return;
        }

        _healthMonitor.Tick();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.KeepScreenOn = false;
        _healthTimer.Stop();
        _mediaPlayer.Stop();
        VideoView.MediaPlayer = null;
        _mediaPlayer.Dispose();
        _libVLC.Dispose();
    }
}