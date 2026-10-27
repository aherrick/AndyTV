using AndyTV.Maui.ViewModels;

namespace AndyTV.Maui.Views;

public partial class ChannelsPage : ContentPage
{
    private readonly ChannelsViewModel _viewModel;

    public ChannelsPage(ChannelsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ChannelsViewModel.Channels))
            {
                Dispatcher.Dispatch(CollapseAllButRecent);
            }
        };
    }

    // Search results stay expanded so matches aren't hidden.
    private void CollapseAllButRecent()
    {
        if (_viewModel.SearchText?.Trim().Length >= 2)
        {
            return;
        }

        channelListView.CollapseAll();
        var recent = channelListView.DataSource.Groups.FirstOrDefault(g => Equals(g.Key, "Recent"));
        if (recent is not null)
        {
            channelListView.ExpandGroup(recent);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.EnsureChannelsLoaded();
    }
}