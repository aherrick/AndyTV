using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public interface IFavoriteChannelService
{
    /// <summary>
    /// Cached list of favorite channels. Call RefreshFavorites() to update.
    /// </summary>
    List<Channel> Favorites { get; }
    
    /// <summary>
    /// Refreshes the cached favorites from storage.
    /// </summary>
    void RefreshFavorites();
    
    List<Channel> LoadFavoriteChannels();
    void SaveFavoriteChannels(IEnumerable<Channel> channels);
    void AddFavorite(Channel channel);
    void RemoveFavorite(Channel channel);
    bool IsFavorite(Channel channel);
}
