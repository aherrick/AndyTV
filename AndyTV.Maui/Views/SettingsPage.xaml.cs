using AndyTV.Maui.ViewModels;
using Syncfusion.Maui.ListView;

namespace AndyTV.Maui.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        playlistListView.ItemDragging += OnItemDragging;
    }

    private void OnItemDragging(object _, ItemDraggingEventArgs e)
    {
        if (e.Action == DragAction.Drop)
        {
            _viewModel.SaveCurrentOrder();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Initialize();
    }
}