using AndyTV.Data.Models;
using AndyTV.Data.Services;
using AndyTV.Maui.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndyTV.Maui.ViewModels;

public partial class ChannelsViewModel(
    IPlaylistService playlistService,
    IRecentChannelService recentChannelService,
    IFavoriteChannelService favoriteChannelService,
    ILastChannelService lastChannelService,
    ILocalConfigService localConfigService
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
                _ = FilterChannels(value);
            }
        }
    }

    private const int SearchDelayMilliseconds = 250;
    private const int MaxSearchResults = 500;

    // Shown when not searching; search also covers search-only playlists (e.g. huge VOD lists).
    private List<Channel> _listChannels = [];
    private List<Channel> _recentChannels = [];
    private int _searchVersion;
    private bool _hasLoaded;

    // Swapped as a whole list so the grouped list view refreshes once instead of per item.
    [ObservableProperty]
    public partial List<Channel> Channels { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UseLocalColor))]
    public partial bool UseLocal { get; set; }

    partial void OnUseLocalChanged(bool value)
    {
        var config = localConfigService.Load();
        config.Enabled = value;
        localConfigService.Save(config);
    }

    public Color UseLocalColor => UseLocal ? Colors.LimeGreen : Colors.Gray;

    public async Task EnsureChannelsLoaded()
    {
        UseLocal = localConfigService.Load().Enabled;

        if (_hasLoaded && _listChannels.Count > 0)
        {
            return;
        }

        // Use channels pre-fetched by the background startup refresh; otherwise fetch now
        if (playlistService.Channels.Count > 0)
        {
            Populate();
        }
        else
        {
            // Let the resumed channel start buffering before playlist downloads compete for bandwidth.
            if (lastChannelService.LoadLastChannel() is not null)
            {
                await Task.WhenAny(Views.PlayerPage.FirstPlaying.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            }
            await LoadChannels();
        }
    }

    // Debounced and run off the UI thread; a newer keystroke discards older results.
    private async Task FilterChannels(string text)
    {
        var version = ++_searchVersion;
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2)
        {
            Channels = _listChannels;
            return;
        }

        await Task.Delay(SearchDelayMilliseconds);
        if (version != _searchVersion)
        {
            return;
        }

        // Read live so search-only playlists still loading in the background get picked up.
        var source = _recentChannels.Concat(playlistService.PlaylistChannels.SelectMany(x => x.Channels));
        var results = await Task.Run(() =>
            source
                .Where(c => c.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                .Take(MaxSearchResults)
                .ToList()
        );
        if (version == _searchVersion)
        {
            Channels = results;
        }
    }

    [RelayCommand]
    private void ToggleUseLocal() => UseLocal = !UseLocal;

    private void Populate()
    {
        _recentChannels = recentChannelService.GetRecentChannels();
        foreach (var ch in _recentChannels)
        {
            ch.Category = "Recent";
        }

        _listChannels =
        [
            .. _recentChannels,
            .. playlistService.PlaylistChannels.Where(x => x.Playlist.ShowInMenu).SelectMany(x => x.Channels),
        ];
        SearchText = string.Empty;
        Channels = _listChannels;
        _hasLoaded = true;
    }

    [RelayCommand]
    private async Task LoadChannels()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            // Off the UI thread: parsing large M3U playlists otherwise freezes the app
            await Task.Run(() => playlistService.RefreshMenuChannelsFirst());
            Populate();
            await Toast.Make($"Loaded {_listChannels.Count} channels").Show();
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
        {
            return;
        }

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
        {
            return;
        }

        await Shell.Current.Navigation.PushAsync(new Views.PlayerPage(channel));
    }
}
