using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AndyTV.Data.Models;
using AndyTV.Data.Services;

namespace AndyTV.Maui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IPlaylistService _playlistService;

    [ObservableProperty]
    public partial string PlaylistName { get; set; }

    [ObservableProperty]
    public partial string PlaylistUrl { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public SettingsViewModel(IPlaylistService playlistService)
    {
        _playlistService = playlistService;
        PlaylistName = string.Empty;
        PlaylistUrl = string.Empty;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var playlists = _playlistService.LoadPlaylists();
        if (playlists.Count > 0)
        {
            PlaylistName = playlists[0].Name ?? string.Empty;
            PlaylistUrl = playlists[0].Url ?? string.Empty;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaylistName) || string.IsNullOrWhiteSpace(PlaylistUrl))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Please enter both name and URL", "OK");
            return;
        }

        IsBusy = true;

        try
        {
            var playlist = new Playlist
            {
                Name = PlaylistName.Trim(),
                Url = PlaylistUrl.Trim(),
                ShowInMenu = true
            };

            _playlistService.SavePlaylists([playlist]);

            await Shell.Current.DisplayAlertAsync("Success", "Playlist saved!", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
