using AndyTV.Maui.ViewModels;
using LibVLCSharp.Shared;

namespace AndyTV.Maui.Views;

public partial class PlayerPage : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    private LibVLC _libVLC;
    private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;

    public PlayerPage(PlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // Ensure shell chrome stays hidden when navigating directly
        Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, false);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        DeviceDisplay.Current.KeepScreenOn = true;

        if (!string.IsNullOrEmpty(_viewModel.Url))
        {
            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            VideoView.MediaPlayer = _mediaPlayer;

            var media = new Media(_libVLC, new Uri(_viewModel.Url));
            _mediaPlayer.Play(media);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.KeepScreenOn = false;

        _mediaPlayer?.Stop();
        VideoView.MediaPlayer = null;
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();
    }
}