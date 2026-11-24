using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndyTV.Maui.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string ChannelName { get; set; }

    // "Local · 320p" while the stream goes through the PC; null when it plays direct.
    [ObservableProperty]
    public partial string Route { get; set; }

    // Shown over the video while there's nothing to watch; null once the stream plays.
    [ObservableProperty]
    public partial string Status { get; set; }

    [ObservableProperty]
    public partial string StatusHint { get; set; }

    public void SetStatus(string status, string hint = null)
    {
        Status = status;
        StatusHint = hint;
    }

    [RelayCommand]
    private async Task GoBack() => await Shell.Current.Navigation.PopAsync();
}
