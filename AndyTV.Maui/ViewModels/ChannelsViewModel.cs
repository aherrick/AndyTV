using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using System.Collections.ObjectModel;

namespace AndyTV.Maui.ViewModels;

public partial class ChannelsViewModel : ObservableObject
{
    private readonly IPlaylistService _playlistService;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    public ObservableCollection<ChannelGroup> ChannelGroups { get; } = [];

    public ChannelsViewModel(IPlaylistService playlistService)
    {
        _playlistService = playlistService;
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
            ChannelGroups.Clear();

            // Load from Top US channels
            var topUs = ChannelService.TopUs();
            foreach (var category in topUs.OrderBy(c => c.Key))
            {
                var group = new ChannelGroup(category.Key);
                foreach (var channelTop in category.Value.OrderBy(c => c.Name))
                {
                    group.Add(new Channel
                    {
                        Name = channelTop.Name,
                        Category = category.Key
                    });
                }
                ChannelGroups.Add(group);
            }

            // Refresh and use cached playlist channels
            await _playlistService.RefreshChannelsAsync();
            
            foreach (var (playlist, channels) in _playlistService.PlaylistChannels)
            {
                if (channels.Count == 0) continue;

                var group = new ChannelGroup(playlist.Name);
                foreach (var ch in channels.Take(100)) // Limit for performance
                {
                    group.Add(ch);
                }
                ChannelGroups.Add(group);
            }

            StatusMessage = $"Loaded {ChannelGroups.Sum(g => g.Count)} channels";
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
