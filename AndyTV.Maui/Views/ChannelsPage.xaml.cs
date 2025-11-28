using AndyTV.Maui.ViewModels;

namespace AndyTV.Maui.Views;

public partial class ChannelsPage : ContentPage
{
    private readonly ChannelsViewModel _viewModel;

    public ChannelsPage(ChannelsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadChannelsCommand.ExecuteAsync(null);
    }
}