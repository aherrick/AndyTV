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
    private readonly List<Channel> _allChannels = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Loading channels...";

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    public ObservableCollection<Channel> Channels { get; } = [];

    private partial void OnSearchTextChanged(string value)
    {
        FilterChannels();
    }

    private void FilterChannels()
    {
        Channels.Clear();

        var filtered = _allChannels;

        // Only filter if 2+ characters
        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.Trim().ToLowerInvariant();
            filtered =
            [
                .. _allChannels.Where(c =>
                    (c.Name ?? string.Empty).Contains(
                        search,
                        StringComparison.InvariantCultureIgnoreCase
                    )
                ),
            ];
        }

        foreach (var ch in filtered)
        {
            Channels.Add(ch);
        }

        StatusMessage = $"Showing {Channels.Count} of {_allChannels.Count} channels";
    }

    [RelayCommand]
    private async Task LoadChannelsAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = "Loading channels...";

        try
        {
            _allChannels.Clear();

            // Load playlist channels
            await playlistService.RefreshChannelsAsync();

            // Add Recent
            var recentChannels = recentChannelService.GetRecentChannels();
            foreach (var ch in recentChannels)
            {
                ch.Category = "Recent";
                _allChannels.Add(ch);
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
                    _allChannels.Add(ch);
                }
            }

            // Apply filter (or show all)
            FilterChannels();
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