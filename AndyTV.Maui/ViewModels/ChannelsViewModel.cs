using System.Collections.ObjectModel;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndyTV.Maui.ViewModels;

public partial class ChannelsViewModel(
    IPlaylistService playlistService,
    IRecentChannelService recentChannelService
) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Loading channels...";

    public ObservableCollection<Channel> Channels { get; } = [];

    [RelayCommand]
    private async Task LoadChannelsAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = "Loading channels...";

        try
        {
            Channels.Clear();

            // Load playlist channels
            await playlistService.RefreshChannelsAsync();

            // Add Recent
            var recentChannels = recentChannelService.GetRecentChannels();
            foreach (var ch in recentChannels)
            {
                ch.Category = "Recent";
                Channels.Add(ch);
            }

            // Add Playlists
            foreach (var (playlist, channels) in playlistService.PlaylistChannels)
            {
                if (channels == null || channels.Count == 0)
                    continue;

                foreach (var ch in channels)
                {
                    if (ch == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(ch.Name) && string.IsNullOrWhiteSpace(ch.Url))
                        continue;

                    if (string.IsNullOrWhiteSpace(ch.Name))
                        ch.Name = "Channel";

                    ch.Category = playlist.Name ?? "Playlist";
                    Channels.Add(ch);
                }
            }

            StatusMessage = $"Loaded {Channels.Count} channels";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectChannelAsync(Channel channel)
    {
        if (channel == null || string.IsNullOrEmpty(channel.Url))
            return;

        // Add to recent channels
        recentChannelService.AddOrPromote(channel);

        await Shell.Current.GoToAsync(
            $"player?url={Uri.EscapeDataString(channel.Url)}&name={Uri.EscapeDataString(channel.DisplayName)}"
        );
    }
}