using AndyTV.Data.Services;
using AndyTV.Maui.Messages;
using AndyTV.Maui.Services;
using AndyTV.Maui.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;

namespace AndyTV.Maui.Views;

public partial class PlayerPage
    : ContentPage,
        IRecipient<AppResumedMessage>,
        IRecipient<AppStoppedMessage>,
        IRecipient<AudioInterruptedMessage>,
        IRecipient<AudioResumableMessage>
{
    private readonly PlayerViewModel _viewModel;
    private readonly LibVLC _libVLC;
    private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
    private readonly IDispatcherTimer _healthTimer;
    private readonly StreamHealthMonitor _healthMonitor = new();
    private readonly IRemoteCommandService _remoteCommandService;
    private readonly LocalPlaybackService _localPlaybackService;
    private readonly OrientationLockService _orientationLockService;
    private readonly IDispatcherTimer _controlsTimer;

    // Completes when the first stream starts so the startup channel download doesn't compete with it.
    public static readonly TaskCompletionSource FirstPlaying = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private const int HealthCheckMilliseconds = 1000;
    private const int ControlsHideMilliseconds = 3000;

    private int _backgroundVideoTrack = -1;
    private bool _inBackground;
    private bool _needsRestart;

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

        _libVLC = IPlatformApplication.Current.Services.GetRequiredService<LibVLC>();
        _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
        _mediaPlayer.Playing += (_, _) => FirstPlaying.TrySetResult();
        VideoView.MediaPlayer = _mediaPlayer;

        _healthTimer = Dispatcher.CreateTimer();
        _healthTimer.Interval = TimeSpan.FromMilliseconds(HealthCheckMilliseconds);
        _healthTimer.Tick += OnHealthTimerTick;

        _controlsTimer = Dispatcher.CreateTimer();
        _controlsTimer.Interval = TimeSpan.FromMilliseconds(ControlsHideMilliseconds);
        _controlsTimer.Tick += OnControlsTimerTick;

        PlayerTapGesture.Tapped += (_, _) => ShowControls();

        Play(url);
        _healthTimer.Start();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _orientationLockService?.ApplyForPlayback();
        ShowControls();
        WeakReferenceMessenger.Default.Register<AppResumedMessage>(this);
        WeakReferenceMessenger.Default.Register<AppStoppedMessage>(this);
        WeakReferenceMessenger.Default.Register<AudioInterruptedMessage>(this);
        WeakReferenceMessenger.Default.Register<AudioResumableMessage>(this);

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
            _inBackground = false;
            _healthTimer.Start();

            // VLC's audio output stays dead after losing the session, so a fresh start is the reliable recovery
            if (_needsRestart || ShouldRestartOnResume())
            {
                Play(_viewModel.Url);
                return;
            }

            // Re-enable the video track we disabled when the app was backgrounded
            if (_backgroundVideoTrack != -1 && _mediaPlayer.VideoTrack == -1)
            {
                _mediaPlayer.SetVideoTrack(_backgroundVideoTrack);
            }
            _backgroundVideoTrack = -1;

            _healthMonitor.Reset();
        });
    }

    public void Receive(AudioInterruptedMessage _) => _needsRestart = true;

    public void Receive(AudioResumableMessage _) => Dispatcher.Dispatch(() => Play(_viewModel.Url));

    public void Receive(AppStoppedMessage _)
    {
        Dispatcher.Dispatch(() =>
        {
            _inBackground = true;
            _healthTimer.Stop();

            // Keep audio playing in the background: disable video so VLC doesn't stall rendering off-screen
            var currentVideoTrack = _mediaPlayer.VideoTrack;
            if (currentVideoTrack != -1)
            {
                _backgroundVideoTrack = currentVideoTrack;
                _mediaPlayer.SetVideoTrack(-1);
            }
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
        // Audio-only in the background (VLC stalls rendering off-screen); restart with video on resume
        _needsRestart = _inBackground;
        _healthMonitor.Reset();
        _mediaPlayer.Stop();
        using var media = new Media(_libVLC, url, FromType.FromLocation);
        if (_inBackground)
        {
            media.AddOption(":no-video");
        }
        _mediaPlayer.Play(media);
    }

    private void OnHealthTimerTick(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel.Url))
        {
            return;
        }

        using var media = _mediaPlayer.Media;
        if (media?.Statistics is { } stats
            && _healthMonitor.IsStalled(stats.PlayedAudioBuffers, stats.DisplayedPictures))
        {
            Play(_viewModel.Url);
        }
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
        OrientationLockService.UseDefaultOrientation();

        if (_remoteCommandService is not null)
        {
            _remoteCommandService.CommandReceived -= OnRemoteCommandReceived;
            _remoteCommandService.Stop();
        }

        WeakReferenceMessenger.Default.Unregister<AppResumedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<AppStoppedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<AudioInterruptedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<AudioResumableMessage>(this);

        _healthTimer.Stop();
        _controlsTimer.Stop();
        _mediaPlayer.Stop();
        VideoView.MediaPlayer = null;
        _mediaPlayer.Dispose();

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
