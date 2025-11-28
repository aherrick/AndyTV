using System.Collections.ObjectModel;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndyTV.Maui.ViewModels;

public partial class SettingsViewModel(IPlaylistService playlistService) : ObservableObject
{
    [ObservableProperty]
    public partial string PlaylistName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PlaylistUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    public ObservableCollection<Playlist> Playlists { get; } = [];

    public void Initialize()
    {
        if (IsLoaded)
            return;
        LoadPlaylists();
        IsLoaded = true;
    }

    private void LoadPlaylists()
    {
        Playlists.Clear();
        var playlists = playlistService.LoadPlaylists();
        foreach (var p in playlists)
        {
            Playlists.Add(p);
        }
    }

    [RelayCommand]
    private async Task AddAsync()
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
                ShowInMenu = true,
            };

            var existing = playlistService.LoadPlaylists();
            existing.Add(playlist);
            playlistService.SavePlaylists(existing);

            // Clear form and reload list
            PlaylistName = string.Empty;
            PlaylistUrl = string.Empty;
            LoadPlaylists();

            await Shell.Current.DisplayAlertAsync("Success", "Playlist added!", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Playlist playlist)
    {
        if (playlist == null)
            return;

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Delete",
            $"Delete '{playlist.Name}'?",
            "Yes",
            "No"
        );
        if (!confirm)
            return;

        var existing = playlistService.LoadPlaylists();
        existing.RemoveAll(p => p.Url == playlist.Url);
        playlistService.SavePlaylists(existing);

        LoadPlaylists();
    }
}