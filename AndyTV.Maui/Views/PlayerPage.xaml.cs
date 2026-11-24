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

    // A stream that never starts gets this many tries, then waits for a tap instead of retrying forever.
    private const int MaxStartAttempts = 3;

    private readonly string _sourceUrl;

    // VLC's Stop blocks until the stream's connection closes, so VLC calls run in order off the UI thread.
    private Task _vlc = Task.CompletedTask;
    private int _backgroundVideoTrack = -1;
    private int _failedStarts;
    private bool _hasPlayed;
    private bool _controlsVisible;
    private bool _closed;
    private bool _inBackground;
    private bool _needsRestart;
    private bool _paused;

    public PlayerPage(Channel channel)
    {
        InitializeComponent();

        _viewModel = new PlayerViewModel { ChannelName = channel.DisplayName };
        BindingContext = _viewModel;
        _sourceUrl = channel.Url;

        DeviceDisplay.Current.KeepScreenOn = true;

        var services = IPlatformApplication.Current.Services;
        services.GetRequiredService<RecentChannelService>().AddOrPromote(channel);
        services.GetRequiredService<LastChannelService>().SaveLastChannel(channel);
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
        _controlsTimer.Tick += (_, _) => HideControls();

        PlayerTapGesture.Tapped += (_, _) => OnPlayerTapped();
        NowPlaying.Register(Play, Pause);

        Play();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ShowControls();
        // Explicit registrations: RegisterAll relies on reflection, which isn't safe under iOS AOT/trimming.
        WeakReferenceMessenger.Default.Register<AppResumedMessage>(this);
        WeakReferenceMessenger.Default.Register<AppStoppedMessage>(this);
        WeakReferenceMessenger.Default.Register<AudioInterruptedMessage>(this);
        WeakReferenceMessenger.Default.Register<AudioResumableMessage>(this);
    }

    public static async Task OpenAsync(Channel channel)
    {
        if (!string.IsNullOrEmpty(channel?.Url))
        {
            await Shell.Current.Navigation.PushAsync(new PlayerPage(channel));
        }
    }

    public void Receive(AppResumedMessage _) =>
        OnUiThread(() =>
        {
            _inBackground = false;
            if (_paused)
            {
                return;
            }
            _healthTimer.Start();

            // VLC's audio output stays dead after losing the session, so a fresh start is the reliable recovery
            if (_needsRestart || ShouldRestartOnResume())
            {
                Play();
                return;
            }

            // Re-enable the video track we disabled when the app was backgrounded
            RunVlc(() =>
            {
                if (_backgroundVideoTrack != -1 && _mediaPlayer.VideoTrack == -1)
                {
                    _mediaPlayer.SetVideoTrack(_backgroundVideoTrack);
                }
                _backgroundVideoTrack = -1;
            });

            _healthMonitor.Reset();
        });

    public void Receive(AudioInterruptedMessage _) => _needsRestart = true;

    public void Receive(AudioResumableMessage _) =>
        OnUiThread(() =>
        {
            if (!_paused)
            {
                Play();
            }
        });

    public void Receive(AppStoppedMessage _) =>
        OnUiThread(() =>
        {
            _inBackground = true;
            _healthTimer.Stop();

            // Keep audio playing in the background: disable video so VLC doesn't stall rendering off-screen
            RunVlc(() =>
            {
                var videoTrack = _mediaPlayer.VideoTrack;
                if (videoTrack != -1)
                {
                    _backgroundVideoTrack = videoTrack;
                    _mediaPlayer.SetVideoTrack(-1);
                }
            });
        });

    // Messages arrive off the UI thread and can land just after the page closed.
    private void OnUiThread(Action action) =>
        Dispatcher.Dispatch(() =>
        {
            if (!_closed)
            {
                action();
            }
        });

    private void RunVlc(Action action) => _vlc = _vlc.ContinueWith(_ => action(), TaskScheduler.Default);

    private bool ShouldRestartOnResume() =>
        _mediaPlayer.State is VLCState.NothingSpecial or VLCState.Stopped or VLCState.Ended or VLCState.Error;

    // Every (re)start gets a new server session, which makes the server pop a fresh stream.
    private void Play()
    {
        if (_closed)
        {
            return;
        }

        var (url, quality) = _localPlaybackService.GetStream(_sourceUrl);
        _viewModel.Route = quality is null ? null : $"Local · {quality}p";
        _viewModel.SetStatus(_hasPlayed ? "Reconnecting…" : "Connecting…");
        _paused = false;

        // Audio-only in the background (VLC stalls rendering off-screen); restart with video on resume
        var inBackground = _inBackground;
        _needsRestart = inBackground;
        _healthMonitor.Reset();
        RunVlc(() =>
        {
            _mediaPlayer.Stop();
            _backgroundVideoTrack = -1;
            using var media = new Media(_libVLC, url, FromType.FromLocation);
            if (inBackground)
            {
                media.AddOption(":no-video");
            }
            _mediaPlayer.Play(media);
        });

        // Off-screen health checks stay off (VLC stalls rendering in the background).
        if (!inBackground)
        {
            _healthTimer.Start();
        }
        NowPlaying.Update(_viewModel.ChannelName, playing: true);
    }

    // Live streams can't pause, so lock-screen pause stops; play restarts with a fresh session.
    private void Pause() => StopPlayback("Paused", "Tap to play");

    private void StopPlayback(string status, string hint)
    {
        if (_closed)
        {
            return;
        }

        _paused = true;
        _failedStarts = 0;
        _healthTimer.Stop();
        RunVlc(_mediaPlayer.Stop);
        _viewModel.SetStatus(status, hint);
        NowPlaying.Update(_viewModel.ChannelName, playing: false);
    }

    private void OnHealthTimerTick(object sender, EventArgs e)
    {
        // Skip while VLC is still busy so checks and restarts never pile up behind a slow call.
        if (!_vlc.IsCompleted)
        {
            return;
        }

        using var media = _mediaPlayer.Media;
        if (media?.Statistics is not { } stats)
        {
            return;
        }

        var stalled = _healthMonitor.IsStalled(stats.PlayedAudioBuffers, stats.DisplayedPictures);
        if (_healthMonitor.HasStarted)
        {
            _hasPlayed = true;
            _failedStarts = 0;
            _viewModel.SetStatus(null);
        }

        if (!stalled)
        {
            return;
        }

        if (!_healthMonitor.HasStarted && ++_failedStarts >= MaxStartAttempts)
        {
            StopPlayback("Channel isn't responding", "Tap to retry");
            return;
        }
        Play();
    }

    // A tap starts a stopped stream; otherwise it shows or hides the controls.
    private void OnPlayerTapped()
    {
        if (_paused)
        {
            Play();
            ShowControls();
        }
        else if (_controlsVisible)
        {
            HideControls();
        }
        else
        {
            ShowControls();
        }
    }

    private void ShowControls()
    {
        SetControlsVisible(true);
        _controlsTimer.Stop();
        _controlsTimer.Start();
    }

    private void HideControls()
    {
        _controlsTimer.Stop();
        SetControlsVisible(false);
    }

    private void SetControlsVisible(bool visible)
    {
        _controlsVisible = visible;
        TopBar.Opacity = visible ? 1 : 0;
        BackButton.InputTransparent = !visible;
        AirPlayBorder.InputTransparent = !visible;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _closed = true;
        DeviceDisplay.Current.KeepScreenOn = false;
        NowPlaying.Clear();

        WeakReferenceMessenger.Default.UnregisterAll(this);

        _healthTimer.Stop();
        _controlsTimer.Stop();

        // The view lets go of the player here; VLC only reads it when a stream starts, so the
        // running one is stopped and released in the background after anything already queued.
        VideoView.MediaPlayer = null;
        RunVlc(() =>
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
        });
    }
}
