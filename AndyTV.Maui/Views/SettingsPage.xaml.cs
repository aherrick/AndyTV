using AndyTV.Data.Models;
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

    private void OnSearchOnlyToggled(object sender, ToggledEventArgs e)
    {
        if (sender is Switch { BindingContext: Playlist playlist })
        {
            _viewModel.SetSearchOnly(playlist, e.Value);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Initialize();
    }
}