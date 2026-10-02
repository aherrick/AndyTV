using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class FavoriteChannelService(IStorageProvider storage) : IFavoriteChannelService
{
    private const string FavoriteChannelsFile = "favorite_channels.json";

    // Cached until the next save.
    private List<Channel> _favorites;
    private HashSet<string> _urls;

    public List<Channel> Favorites => _favorites ??= LoadFavoriteChannels();

    public List<Channel> LoadFavoriteChannels() =>
        storage.ReadJson<List<Channel>>(FavoriteChannelsFile) ?? [];

    public void SaveFavoriteChannels(IEnumerable<Channel> channels)
    {
        storage.WriteJson(FavoriteChannelsFile, channels);
        _favorites = null;
        _urls = null;
    }

    public void AddFavorite(Channel channel)
    {
        if (!string.IsNullOrWhiteSpace(channel?.Url) && !IsFavorite(channel))
        {
            SaveFavoriteChannels([.. LoadFavoriteChannels(), channel]);
        }
    }

    public void RemoveFavorite(Channel channel)
    {
        if (string.IsNullOrWhiteSpace(channel?.Url))
        {
            return;
        }

        var favorites = LoadFavoriteChannels();
        favorites.RemoveAll(f => string.Equals(f.Url, channel.Url, StringComparison.OrdinalIgnoreCase));
        SaveFavoriteChannels(favorites);
    }

    public bool IsFavorite(Channel channel)
    {
        if (string.IsNullOrWhiteSpace(channel?.Url))
        {
            return false;
        }

        _urls ??= new HashSet<string>(
            Favorites.Where(f => !string.IsNullOrWhiteSpace(f.Url)).Select(f => f.Url.Trim()),
            StringComparer.OrdinalIgnoreCase);
        return _urls.Contains(channel.Url.Trim());
    }
}