using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndyTV.Maui.ViewModels;

[QueryProperty(nameof(Url), "url")]
[QueryProperty(nameof(ChannelName), "name")]
public partial class PlayerViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Url { get; set; }

    [ObservableProperty]
    public partial string ChannelName { get; set; }

    public PlayerViewModel()
    {
        Url = string.Empty;
        ChannelName = string.Empty;
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
