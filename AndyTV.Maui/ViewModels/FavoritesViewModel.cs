using System.Collections.ObjectModel;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndyTV.Maui.ViewModels;

public partial class FavoritesViewModel(
    IFavoriteChannelService favoriteChannelService,
    IRecentChannelService recentChannelService
) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    public ObservableCollection<Channel> Favorites { get; } = [];

    [RelayCommand]
    private void LoadFavorites()
    {
        try
        {
            Favorites.Clear();
            var favorites = favoriteChannelService.LoadFavoriteChannels();
            foreach (var channel in favorites)
            {
                channel.Category = "Favorite";
                Favorites.Add(channel);
            }
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
            return;

        recentChannelService.AddOrPromote(channel);

        var playerPage = new Views.PlayerPage(channel.Url, channel.DisplayName);
        await Shell.Current.Navigation.PushModalAsync(playerPage);
    }
}