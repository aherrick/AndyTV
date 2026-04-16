using LibVLCSharp.Shared;

namespace AndyTV.Maui
{
    public partial class MainPage : ContentPage
    {
        LibVLC _libVLC;
        MediaPlayer _mediaPlayer;

        public MainPage()
        {
            InitializeComponent();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
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
