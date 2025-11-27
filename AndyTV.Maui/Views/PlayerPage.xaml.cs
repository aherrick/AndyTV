using AndyTV.Maui.ViewModels;
using CommunityToolkit.Maui.Views;

namespace AndyTV.Maui.Views;

public partial class PlayerPage : ContentPage
{
    private readonly PlayerViewModel _viewModel;

    public PlayerPage(PlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!string.IsNullOrEmpty(_viewModel.Url))
        {
            MediaElement.Source = MediaSource.FromUri(_viewModel.Url);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MediaElement.Stop();
        MediaElement.Handler?.DisconnectHandler();
    }
}