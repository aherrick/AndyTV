using AndyTV.Maui.ViewModels;

namespace AndyTV.Maui.Views;

public partial class PlayerPage : ContentPage
{
    private readonly PlayerViewModel _viewModel;

    public PlayerPage(string url, string channelName)
    {
        InitializeComponent();

        _viewModel = new PlayerViewModel { Url = url, ChannelName = channelName };
        BindingContext = _viewModel;

        DeviceDisplay.Current.KeepScreenOn = true;

        VideoPlayer.Source = url;
        VideoPlayer.Play();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.KeepScreenOn = false;
        VideoPlayer.Stop();
    }
}