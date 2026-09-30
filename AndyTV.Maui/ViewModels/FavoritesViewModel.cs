using System.Collections.ObjectModel;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndyTV.Maui.ViewModels;

public partial class FavoritesViewModel(
    IFavoriteChannelService favoriteChannelService,
    IRecentChannelService recentChannelService,
    ILastChannelService lastChannelService
) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Channel> Favorites { get; set; } = [];

    [RelayCommand]
    private void LoadFavorites()
    {
        try
        {
            var favorites = favoriteChannelService.LoadFavoriteChannels();
            foreach (var channel in favorites)
            {
                channel.Category = "Favorite";
            }
            Favorites = new ObservableCollection<Channel>(favorites);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RemoveFavorite(Channel channel)
    {
        if (channel == null)
            return;

        favoriteChannelService.RemoveFavorite(channel);
        Favorites.Remove(channel);
        await Toast.Make("Removed from favorites").Show();
    }

    [RelayCommand]
    private async Task SelectChannel(Channel channel)
    {
        if (channel == null || string.IsNullOrEmpty(channel.Url))
        {
            return;
        }

        recentChannelService.AddOrPromote(channel);
        lastChannelService.SaveLastChannel(channel);

        await Shell.Current.Navigation.PushAsync(new Views.PlayerPage(channel.Url, channel.DisplayName));
    }
}