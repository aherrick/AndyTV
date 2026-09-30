using AndyTV.Data.Models;
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
    private readonly LocalPlaybackService _localPlaybackService;
    private readonly IDispatcherTimer _controlsTimer;

    // Completes when the first stream starts so the startup channel download doesn't compete with it.
    public static readonly TaskCompletionSource FirstPlaying = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private const int HealthCheckMilliseconds = 1000;
    private const int ControlsHideMilliseconds = 3000;

    private readonly string _sourceUrl;
    private bool _starting;
    private int _backgroundVideoTrack = -1;
    private bool _inBackground;
    private bool _needsRestart;
    private bool _closed;

    public PlayerPage(Channel channel)
    {
        InitializeComponent();

        _viewModel = new PlayerViewModel { ChannelName = channel.DisplayName };
        BindingContext = _viewModel;
        _sourceUrl = channel.Url;

        DeviceDisplay.Current.KeepScreenOn = true;

        var services = IPlatformApplication.Current.Services;
        services.GetRequiredService<IRecentChannelService>().AddOrPromote(channel);
        services.GetRequiredService<ILastChannelService>().SaveLastChannel(channel);
        _localPlaybackService = services.GetRequiredService<LocalPlaybackService>();
        _libVLC = services.GetRequiredService<LibVLC>();
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

        Play();
        _healthTimer.Start();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ShowControls();
        WeakReferenceMessenger.Default.Register<AppResumedMessage>(this);
        WeakReferenceMessenger.Default.Register<AppStoppedMessage>(this);
        WeakReferenceMessenger.Default.Register<AudioInterruptedMessage>(this);
        WeakReferenceMessenger.Default.Register<AudioResumableMessage>(this);
    }

    public void Receive(AppResumedMessage _)
    {
        Dispatcher.Dispatch(() =>
        {
            _inBackground = false;
            _healthTimer.Start();

            // VLC's audio output stays dead after losing the session, so a fresh start is the reliable recovery
            if (_needsRestart || ShouldRestartOnResume())
            {
                Play();
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

    public void Receive(AudioResumableMessage _) => Dispatcher.Dispatch(Play);

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

    // Every (re)start pops a fresh server stream; the server kills the previous one first.
    private async void Play()
    {
        // One start at a time; it picks up the current background state when it finishes
        if (_starting)
        {
            return;
        }
        _starting = true;
        _mediaPlayer.Stop();

        var url = await _localPlaybackService.Start(_sourceUrl);
        _starting = false;
        if (_closed)
        {
            return;
        }

        _viewModel.Url = url;

        // Audio-only in the background (VLC stalls rendering off-screen); restart with video on resume
        _needsRestart = _inBackground;
        _healthMonitor.Reset();
        using var media = new Media(_libVLC, url, FromType.FromLocation);
        if (_inBackground)
        {
            media.AddOption(":no-video");
        }
        _mediaPlayer.Play(media);
    }

    private void OnHealthTimerTick(object sender, EventArgs e)
    {
        using var media = _mediaPlayer.Media;
        if (media?.Statistics is { } stats
            && _healthMonitor.IsStalled(stats.PlayedAudioBuffers, stats.DisplayedPictures))
        {
            Play();
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
        BackButton.Opacity = 1;
        BackButton.InputTransparent = false;
        _controlsTimer.Stop();
        _controlsTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _closed = true;
        DeviceDisplay.Current.KeepScreenOn = false;

        WeakReferenceMessenger.Default.Unregister<AppResumedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<AppStoppedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<AudioInterruptedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<AudioResumableMessage>(this);

        _healthTimer.Stop();
        _controlsTimer.Stop();
        _mediaPlayer.Stop();
        VideoView.MediaPlayer = null;
        _mediaPlayer.Dispose();
    }
}
