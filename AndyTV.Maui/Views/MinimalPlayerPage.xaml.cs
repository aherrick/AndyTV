using LibVLCSharp.MAUI;
using LibVLCSharp.Shared;
using VlcMediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace AndyTV.Maui.Views;

public partial class MinimalPlayerPage : ContentPage
{
    private const string TestUrl =
        "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";

    private LibVLC _libVlc;
    private Media _media;
    private VlcMediaPlayer _mediaPlayer;
    private bool _isPageVisible;
    private bool _isVideoViewReady;

    public MinimalPlayerPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _isPageVisible = true;

        if (_mediaPlayer == null)
        {
            _libVlc = new LibVLC(enableDebugLogs: true);
            _media = new Media(_libVlc, new Uri(TestUrl));
            _mediaPlayer = new VlcMediaPlayer(_libVlc)
            {
                EnableHardwareDecoding = true,
                Media = _media,
            };

            VideoView.MediaPlayer = _mediaPlayer;
        }

        TryPlay();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _isPageVisible = false;
        _isVideoViewReady = false;

        VideoView.MediaPlayer = null;

        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _media?.Dispose();
        _libVlc?.Dispose();

        _mediaPlayer = null;
        _media = null;
        _libVlc = null;
    }

    private void OnVideoViewMediaPlayerChanged(object sender, MediaPlayerChangedEventArgs e)
    {
        _isVideoViewReady = true;
        TryPlay();
    }

    private void TryPlay()
    {
        if (_isPageVisible && _isVideoViewReady && _mediaPlayer != null && !_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Play();
        }
    }
}