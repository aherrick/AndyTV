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
    private bool _isBusy;

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

    // One line per search-only playlist still loading or failed; loaded ones just show up in search.
    [ObservableProperty]
    public partial string SearchOnlyStatus { get; set; }

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
                try
                {
                    await Views.PlayerPage.FirstPlaying.Task.WaitAsync(TimeSpan.FromSeconds(10));
                }
                catch (TimeoutException) { }
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
        SearchOnlyStatus = BuildSearchOnlyStatus();
        _hasLoaded = true;
    }

    private string BuildSearchOnlyStatus()
    {
        var loaded = playlistService.PlaylistChannels;
        var lines = playlistService
            .LoadPlaylists()
            .Where(p => !p.ShowInMenu)
            .Select(p => loaded.Find(x => x.Playlist.Url == p.Url && x.Playlist.Name == p.Name).Channels switch
            {
                null => $"{p.Name}: Loading…",
                [] => $"{p.Name}: Failed to load",
                _ => null,
            })
            .Where(line => line is not null);
        return string.Join(Environment.NewLine, lines);
    }

    private async Task UpdateWhenSearchOnlyLoaded(Task searchOnlyLoaded)
    {
        await searchOnlyLoaded;
        SearchOnlyStatus = BuildSearchOnlyStatus();
        await FilterChannels(SearchText);
    }

    [RelayCommand]
    private async Task LoadChannels()
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;

        try
        {
            // Off the UI thread: parsing large M3U playlists otherwise freezes the app
            var searchOnlyLoaded = await Task.Run(() => playlistService.RefreshMenuChannelsFirst());
            Populate();
            _ = UpdateWhenSearchOnlyLoaded(searchOnlyLoaded);
            await Toast.Make($"Loaded {_listChannels.Count} channels").Show();
        }
        catch (Exception ex)
        {
            await Toast.Make($"Error: {ex.Message}").Show();
        }
        finally
        {
            _isBusy = false;
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
    private Task SelectChannel(Channel channel) => Views.PlayerPage.OpenAsync(channel);
}
