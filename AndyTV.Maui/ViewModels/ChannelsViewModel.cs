using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using System.Collections.ObjectModel;

namespace AndyTV.Maui.ViewModels;

public partial class ChannelsViewModel : ObservableObject
{
    private readonly IPlaylistService _playlistService;
    private readonly IRecentChannelService _recentChannelService;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    public ObservableCollection<Channel> Channels { get; } = [];

    public ChannelsViewModel(IPlaylistService playlistService, IRecentChannelService recentChannelService)
    {
        _playlistService = playlistService;
        _recentChannelService = recentChannelService;
        StatusMessage = "Loading channels...";
    }

    [RelayCommand]
    private async Task LoadChannelsAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Loading channels...";

        try
        {
            Channels.Clear();

            // Load all data first
            var recentChannels = _recentChannelService.GetRecentChannels();
            var topUs = ChannelService.TopUs();
            await _playlistService.RefreshChannelsAsync();

            // Build a flat list
            var allChannels = new List<Channel>();

            // Add Recent
            foreach (var ch in recentChannels)
            {
                ch.Category = "Recent";
                allChannels.Add(ch);
            }

            // Add Top US
            foreach (var category in topUs.OrderBy(c => c.Key))
            {
                foreach (var channelTop in category.Value.OrderBy(c => c.Name))
                {
                    allChannels.Add(new Channel
                    {
                        Name = channelTop.Name,
                        Category = category.Key
                    });
                }
            }

            // Add Playlists
            foreach (var (playlist, channels) in _playlistService.PlaylistChannels)
            {
                if (channels == null || channels.Count == 0)
                    continue;

                foreach (var ch in channels.Take(100))
                {
                    if (ch == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(ch.Name) && string.IsNullOrWhiteSpace(ch.Url))
                        continue;

                    if (string.IsNullOrWhiteSpace(ch.Name))
                        ch.Name = "Channel";

                    ch.Category = playlist.Name ?? "Playlist";
                    allChannels.Add(ch);
                }
            }

            // Add all at once
            foreach (var ch in allChannels)
            {
                Channels.Add(ch);
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
        if (channel == null || string.IsNullOrEmpty(channel.Url)) return;

        // Add to recent channels
        _recentChannelService.AddOrPromote(channel);

        await Shell.Current.GoToAsync($"player?url={Uri.EscapeDataString(channel.Url)}&name={Uri.EscapeDataString(channel.DisplayName)}");
    }
}

public class ChannelGroup : ObservableCollection<Channel>
{
    public string Name { get; }

    public ChannelGroup(string name)
    {
        Name = name;
    }
}
