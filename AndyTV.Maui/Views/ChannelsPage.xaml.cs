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
                Dispatcher.Dispatch(UpdateGroups);
            }
        };
    }

    // Search results open every matching group; the full list starts with only Recent open.
    private void UpdateGroups()
    {
        if (_viewModel.SearchText?.Trim().Length >= 2)
        {
            channelListView.ExpandAll();
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