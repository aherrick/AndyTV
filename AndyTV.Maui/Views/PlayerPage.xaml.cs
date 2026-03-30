using AndyTV.Data.Services;
using AndyTV.Maui.ViewModels;

namespace AndyTV.Maui.Views;

public partial class PlayerPage : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    private readonly IDispatcherTimer _healthTimer;
    private readonly StreamHealthMonitor _healthMonitor;

    private const int HealthCheckMilliseconds = 1000;

    public PlayerPage(string url, string channelName)
    {
        InitializeComponent();

        _viewModel = new PlayerViewModel { Url = url, ChannelName = channelName };
        BindingContext = _viewModel;

        DeviceDisplay.Current.KeepScreenOn = true;

        _healthMonitor = new StreamHealthMonitor(
            isPaused: () => VideoPlayer.IsPaused,
            restart: () =>
            {
                if (string.IsNullOrEmpty(_viewModel.Url))
                {
                    return;
                }

                Play(_viewModel.Url);
            }
        );

        VideoPlayer.PlaybackActivity += () => _healthMonitor.MarkActivity();

        _healthTimer = Dispatcher.CreateTimer();
        _healthTimer.Interval = TimeSpan.FromMilliseconds(HealthCheckMilliseconds);
        _healthTimer.Tick += OnHealthTimerTick;

        Play(url);
        _healthTimer.Start();
    }

    private void Play(string url)
    {
        _healthMonitor.MarkActivity();
        VideoPlayer.Source = url;
        VideoPlayer.Play();
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
        VideoPlayer.Stop();
    }
}