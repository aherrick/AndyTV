using LibVLCSharp.Shared;

using LibVlcMediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace AndyTV.Maui
{
    public partial class MainPage : ContentPage
    {
        LibVLC _libVLC;
        LibVlcMediaPlayer _mediaPlayer;

        public MainPage()
        {
            InitializeComponent();

            _libVLC = new LibVLC();
            _mediaPlayer = new LibVlcMediaPlayer(_libVLC);
            VideoView.MediaPlayer = _mediaPlayer;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            using var media = new Media(_libVLC, new Uri("https://streams.videolan.org/streams/mp4/Mr_MrsSmith-h264_aac.mp4"));
            _mediaPlayer.Play(media);
        }
    }
}
