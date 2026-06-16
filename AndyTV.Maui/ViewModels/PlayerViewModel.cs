using AndyTV.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndyTV.Maui.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Url { get; set; }

    [ObservableProperty]
    public partial string ChannelName { get; set; }

    public bool CanGoBack { get; set; } = true;

    [RelayCommand]
    private async Task GoBack()
    {
        if (!CanGoBack)
        {
            return;
        }

        await Shell.Current.Navigation.PopAsync();
    }
}