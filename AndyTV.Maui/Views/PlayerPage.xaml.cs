using AndyTV.Data.Services;
using AndyTV.Maui.Messages;
using AndyTV.Maui.Services;
using AndyTV.Maui.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;

namespace AndyTV.Maui.Views;

public partial class PlayerPage : ContentPage, IRecipient<AppResumedMessage>
{
    private readonly PlayerViewModel _viewModel;
    private readonly LibVLC _libVLC;
    private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
    private readonly IDispatcherTimer _healthTimer;
    private readonly StreamHealthMonitor _healthMonitor;
    private readonly IOrientationLockService _orientationLockService;
    private readonly IRemoteCommandService _remoteCommandService;
    private readonly ILocalConfigService _localConfigService;
    private readonly ILocalPlaybackService _localPlaybackService;

    private const int HealthCheckMilliseconds = 1000;

    public PlayerPage(string url, string channelName)
    {
        InitializeComponent();

        _viewModel = new PlayerViewModel { Url = url, ChannelName = channelName };
        BindingContext = _viewModel;
        _orientationLockService =
            IPlatformApplication.Current?.Services.GetService<IOrientationLockService>();
        _remoteCommandService =
            IPlatformApplication.Current?.Services.GetService<IRemoteCommandService>();
        _localConfigService =
            IPlatformApplication.Current?.Services.GetService<ILocalConfigService>();
        _localPlaybackService =
            IPlatformApplication.Current?.Services.GetService<ILocalPlaybackService>();

        DeviceDisplay.Current.KeepScreenOn = true;

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

        _mediaPlayer.TimeChanged += (_, __) => _healthMonitor.MarkActivity();
        _mediaPlayer.PositionChanged += (_, __) => _healthMonitor.MarkActivity();
        _mediaPlayer.Playing += (_, __) => _healthMonitor.MarkActivity();

        _healthTimer = Dispatcher.CreateTimer();
        _healthTimer.Interval = TimeSpan.FromMilliseconds(HealthCheckMilliseconds);
        _healthTimer.Tick += OnHealthTimerTick;

        Play(url);
        _healthTimer.Start();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _orientationLockService?.ApplyForPlayback();
        WeakReferenceMessenger.Default.Register(this);

        if (_remoteCommandService is not null)
        {
            _remoteCommandService.CommandReceived += OnRemoteCommandReceived;
            _remoteCommandService.Start();
        }
    }

    public void Receive(AppResumedMessage _)
    {
        if (string.IsNullOrEmpty(_viewModel.Url))
        {
            return;
        }

        Dispatcher.Dispatch(() =>
        {
            if (ShouldRestartOnResume())
            {
                Play(_viewModel.Url);
                return;
            }

            _healthMonitor.MarkActivity();
        });
    }

    private bool ShouldRestartOnResume()
    {
        return _mediaPlayer.State
            is VLCState.NothingSpecial
                or VLCState.Stopped
                or VLCState.Ended
                or VLCState.Error;
    }

    private void Play(string url)
    {
        _healthMonitor.MarkActivity();
        _mediaPlayer.Stop();
        _mediaPlayer.Play(new Media(_libVLC, url, FromType.FromLocation));
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
        _orientationLockService?.UseDefaultOrientation();

        if (_remoteCommandService is not null)
        {
            _remoteCommandService.CommandReceived -= OnRemoteCommandReceived;
            _remoteCommandService.Stop();
        }

        WeakReferenceMessenger.Default.Unregister<AppResumedMessage>(this);

        _healthTimer.Stop();
        _mediaPlayer.Stop();
        VideoView.MediaPlayer = null;

        if (IsLocalModeActive())
        {
            _ = _localPlaybackService?.StopPlayback();
        }
    }

    private bool IsLocalModeActive()
    {
        var config = _localConfigService?.Load();
        return config?.Enabled == true && !string.IsNullOrWhiteSpace(config.ServerUrl);
    }

    private void OnRemoteCommandReceived(object sender, RemoteCommandEventArgs e)
    {
        Dispatcher.Dispatch(() =>
        {
            switch (e.Kind)
            {
                case RemoteCommandKind.VolumeUp:
                    AdjustVolume(10);
                    break;
                case RemoteCommandKind.VolumeDown:
                    AdjustVolume(-10);
                    break;
            }
        });
    }

    private void AdjustVolume(int delta)
    {
        var newVolume = Math.Clamp(_mediaPlayer.Volume + delta, 0, 200);
        _mediaPlayer.Volume = newVolume;
    }
}
