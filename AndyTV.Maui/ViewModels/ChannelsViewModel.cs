using System.Collections.ObjectModel;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndyTV.Maui.ViewModels;

public partial class ChannelsViewModel(
    IPlaylistService playlistService,
    IRecentChannelService recentChannelService,
    IFavoriteChannelService favoriteChannelService,
    ILastChannelService lastChannelService
) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    public string SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                FilterChannels();
            }
        }
    } = string.Empty;

    private readonly List<Channel> _allChannels = [];
    private bool _isFirstLoad = true;
    private bool _hasLoaded;

    public ObservableCollection<ChannelGroup> Channels { get; } = [];

    public async Task EnsureChannelsLoaded()
    {
        if (_hasLoaded && Channels.Count > 0)
            return;

        await LoadChannels();
    }

    private void FilterChannels()
    {
        Channels.Clear();

        var filtered =
            string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 2
                ? _allChannels
                : _allChannels.Where(c =>
                        c.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();

        var groups = filtered.GroupBy(c => c.Category)
                             .Select(g => new ChannelGroup(g.Key, g));

        foreach (var group in groups)
        {
            Channels.Add(group);
        }
    }

    [RelayCommand]
    private async Task LoadChannels()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            _allChannels.Clear();
            Channels.Clear();
            SearchText = string.Empty;

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
                foreach (var ch in channels)
                {
                    ch.Category = playlist.Name ?? "Playlist";
                    _allChannels.Add(ch);
                }
            }

            FilterChannels();
            _hasLoaded = true;
            await Toast.Make($"Loaded {_allChannels.Count} channels").Show();

            // Auto-play last channel on first load
            if (_isFirstLoad)
            {
                _isFirstLoad = false;
                var lastChannel = lastChannelService.LoadLastChannel();
                if (lastChannel != null && !string.IsNullOrEmpty(lastChannel.Url))
                {
                    var playerPage = new Views.PlayerPage(lastChannel.Url, lastChannel.DisplayName);
                    await Shell.Current.Navigation.PushModalAsync(playerPage);
                }
            }
        }
        catch (Exception ex)
        {
            await Toast.Make($"Error: {ex.Message}").Show();
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFavorite(Channel channel)
    {
        if (channel == null)
            return;

        if (favoriteChannelService.IsFavorite(channel))
        {
            favoriteChannelService.RemoveFavorite(channel);
            await Toast.Make("Removed from favorites").Show();
        }
        else
        {
            favoriteChannelService.AddFavorite(channel);
            await Toast.Make("Added to favorites").Show();
        }
    }

    [RelayCommand]
    private async Task SelectChannel(Channel channel)
    {
        if (channel == null || string.IsNullOrEmpty(channel.Url))
            return;

        // Add to recent channels
        recentChannelService.AddOrPromote(channel);

        // Save as last channel
        lastChannelService.SaveLastChannel(channel);

        var playerPage = new Views.PlayerPage(channel.Url, channel.DisplayName);
        await Shell.Current.Navigation.PushModalAsync(playerPage);
    }
}

public class ChannelGroup(string name, IEnumerable<Channel> channels) : ObservableCollection<Channel>(channels)
{
    public string Name { get; } = name;
}