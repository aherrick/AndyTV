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
    private readonly IRemoteCommandService _remoteCommandService;
    private readonly LocalPlaybackService _localPlaybackService;
    private readonly OrientationLockService _orientationLockService;
    private readonly IDispatcherTimer _controlsTimer;

    private const int HealthCheckMilliseconds = 1000;
    private const int ControlsHideMilliseconds = 3000;

    public PlayerPage(string url, string channelName)
    {
        InitializeComponent();

        _viewModel = new PlayerViewModel { Url = url, ChannelName = channelName };
        BindingContext = _viewModel;
        _orientationLockService =
            IPlatformApplication.Current?.Services.GetService<OrientationLockService>();
        _remoteCommandService =
            IPlatformApplication.Current?.Services.GetService<IRemoteCommandService>();
        _localPlaybackService =
            IPlatformApplication.Current?.Services.GetService<LocalPlaybackService>();

        // Disable double-tap back when in Portrait lock mode
        if (_orientationLockService?.CurrentLockMode == LockMode.Portrait)
        {
            _viewModel.CanGoBack = false;
        }

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

        _controlsTimer = Dispatcher.CreateTimer();
        _controlsTimer.Interval = TimeSpan.FromMilliseconds(ControlsHideMilliseconds);
        _controlsTimer.Tick += OnControlsTimerTick;

        Play(url);
        _healthTimer.Start();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _orientationLockService?.ApplyForPlayback();
        ShowControls();
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

    private void OnPlayerTapped(object sender, TappedEventArgs e)
    {
        ShowControls();
    }

    private void OnControlsTimerTick(object sender, EventArgs e)
    {
        _controlsTimer.Stop();
        BackButton.Opacity = 0;
        BackButton.InputTransparent = true;
    }

    private void ShowControls()
    {
        if (!_viewModel.CanGoBack)
        {
            return;
        }

        BackButton.Opacity = 1;
        BackButton.InputTransparent = false;
        _controlsTimer.Stop();
        _controlsTimer.Start();
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
        _controlsTimer.Stop();
        _mediaPlayer.Stop();
        VideoView.MediaPlayer = null;

        _ = _localPlaybackService?.StopPlayback();
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
