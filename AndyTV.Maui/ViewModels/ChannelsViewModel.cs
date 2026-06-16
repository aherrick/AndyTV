using System.Collections.ObjectModel;
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
    IOrientationLockService orientationLockService,
    ILocalConfigService localConfigService,
    ILocalPlaybackService localPlaybackService
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
    }

    private readonly List<Channel> _allChannels = [];
    private bool _hasLoaded;

    public ObservableCollection<Channel> Channels { get; } = [];

    public LockMode CurrentLockMode => orientationLockService.CurrentLockMode;

    public string LockGlyph => CurrentLockMode == LockMode.Unlocked ? "\uf09c" : "\uf023";

    public Color LockColor => CurrentLockMode switch
    {
        LockMode.Landscape => Colors.Orange,
        LockMode.Portrait => Colors.Red,
        _ => Colors.Gray
    };

    private bool _useLocal;

    public bool UseLocal
    {
        get => _useLocal;
        set
        {
            if (!SetProperty(ref _useLocal, value))
            {
                return;
            }

            OnPropertyChanged(nameof(UseLocalColor));

            // Persist the toggle
            var config = localConfigService.Load();
            config.Enabled = value;
            localConfigService.Save(config);
        }
    }

    public Color UseLocalColor => UseLocal ? Colors.LimeGreen : Colors.Gray;

    public async Task EnsureChannelsLoaded()
    {
        OnPropertyChanged(nameof(CurrentLockMode));
        OnPropertyChanged(nameof(LockGlyph));
        OnPropertyChanged(nameof(LockColor));
        _useLocal = localConfigService.Load().Enabled;
        OnPropertyChanged(nameof(UseLocal));
        OnPropertyChanged(nameof(UseLocalColor));
        orientationLockService.UseDefaultOrientation();

        if (_hasLoaded && Channels.Count > 0)
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
            await LoadChannels();
        }
    }

    private void FilterChannels()
    {
        Channels.Clear();

        var filtered =
            string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 2
                ? _allChannels
                :
                [
                    .. _allChannels.Where(c =>
                        c.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true
                    ),
                ];

        foreach (var ch in filtered)
        {
            Channels.Add(ch);
        }
    }

    [RelayCommand]
    private void ToggleLandscapeLock()
    {
        orientationLockService.CycleLockMode();
        OnPropertyChanged(nameof(CurrentLockMode));
        OnPropertyChanged(nameof(LockGlyph));
        OnPropertyChanged(nameof(LockColor));
    }

    [RelayCommand]
    private void ToggleUseLocal()
    {
        if (UseLocal)
        {
            _ = localPlaybackService.StopPlayback();
        }

        UseLocal = !UseLocal;
    }

    private void Populate()
    {
        _allChannels.Clear();
        Channels.Clear();
        SearchText = string.Empty;

        var recentChannels = recentChannelService.GetRecentChannels();
        foreach (var ch in recentChannels)
        {
            ch.Category = "Recent";
            _allChannels.Add(ch);
        }

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
            // Pull-to-refresh always fetches fresh data from network
            await playlistService.RefreshChannelsAsync();
            Populate();
            await Toast.Make($"Loaded {_allChannels.Count} channels").Show();
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

        // Add to recent channels
        recentChannelService.AddOrPromote(channel);

        // Save as last channel
        lastChannelService.SaveLastChannel(channel);

        var playbackUrl = await localPlaybackService.ResolvePlaybackUrl(channel.Url);

        var playerPage = new Views.PlayerPage(playbackUrl, channel.DisplayName);
        await Shell.Current.Navigation.PushAsync(playerPage);
    }
}
