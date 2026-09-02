using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public interface IFavoriteChannelService
{
    /// <summary>
    /// Cached list of favorite channels, refreshed automatically on save.
    /// </summary>
    List<Channel> Favorites { get; }

    List<Channel> LoadFavoriteChannels();

    void SaveFavoriteChannels(IEnumerable<Channel> channels);

    void AddFavorite(Channel channel);

    void RemoveFavorite(Channel channel);

    bool IsFavorite(Channel channel);
}