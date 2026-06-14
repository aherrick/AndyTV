using System.Collections.ObjectModel;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndyTV.Maui.ViewModels;

public partial class SettingsViewModel(
    IPlaylistService playlistService,
    ILocalConfigService localConfigService
) : ObservableObject
{
    [ObservableProperty]
    public partial string PlaylistName { get; set; }

    [ObservableProperty]
    public partial string PlaylistUrl { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    public partial string LocalServerUrl { get; set; }

    [ObservableProperty]
    public partial string SelectedQuality { get; set; }

    public static string[] QualityOptions { get; } = ["240", "320", "480", "576", "720"];

    public static string AppVersion => $"v{AppInfo.Current.VersionString}";

    public ObservableCollection<Playlist> Playlists { get; } = [];

    public void Initialize()
    {
        if (IsLoaded)
        {
            return;
        }

        LoadPlaylists();
        LoadLocalConfig();
        IsLoaded = true;
    }

    private void LoadLocalConfig()
    {
        var config = localConfigService.Load();
        LocalServerUrl = config.ServerUrl;
        SelectedQuality = string.IsNullOrEmpty(config.Quality) ? "320" : config.Quality;
    }

    public void SaveLocalConfig()
    {
        var config = localConfigService.Load();
        config.ServerUrl = LocalServerUrl?.Trim();
        config.Quality = SelectedQuality;
        localConfigService.Save(config);
    }

    partial void OnLocalServerUrlChanged(string value)
    {
        if (IsLoaded)
        {
            SaveLocalConfig();
        }
    }

    partial void OnSelectedQualityChanged(string value)
    {
        if (IsLoaded)
        {
            SaveLocalConfig();
        }
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
    private async Task Add()
    {
        if (string.IsNullOrWhiteSpace(PlaylistName) || string.IsNullOrWhiteSpace(PlaylistUrl))
        {
            await Shell.Current.DisplayAlertAsync(
                "Error",
                "Please enter both a name and an M3U source.",
                "OK"
            );
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

    public void SaveCurrentOrder()
    {
        playlistService.SavePlaylists([.. Playlists]);
    }

    [RelayCommand]
    private async Task Delete(Playlist playlist)
    {
        if (playlist == null)
        {
            return;
        }

        var confirm = await Shell.Current.DisplayAlertAsync(
            "Delete",
            $"Delete '{playlist.Name}'?",
            "Yes",
            "No"
        );
        if (!confirm)
        {
            return;
        }

        var existing = playlistService.LoadPlaylists();
        existing.RemoveAll(p => p.Url == playlist.Url);
        playlistService.SavePlaylists(existing);

        LoadPlaylists();
    }
}