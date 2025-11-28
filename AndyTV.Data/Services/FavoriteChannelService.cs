using System.Text.Json;
using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class FavoriteChannelService : IFavoriteChannelService
{
    private const string FavoriteChannelsFile = "favorite_channels.json";

    private readonly IStorageProvider _storageProvider;
    
    // Cached favorites list
    public List<Channel> Favorites { get; private set; } = [];
    
    // Fast lookup cache for URLs
    private readonly HashSet<string> _urlCache = new(StringComparer.OrdinalIgnoreCase);

    public FavoriteChannelService(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
        RefreshFavorites();
    }
    
    public void RefreshFavorites()
    {
        Favorites = LoadFavoriteChannels();
        RebuildUrlCache();
    }
    
    private void RebuildUrlCache()
    {
        _urlCache.Clear();
        foreach (var f in Favorites)
        {
            var url = f.Url?.Trim();
            if (!string.IsNullOrWhiteSpace(url))
            {
                _urlCache.Add(url);
            }
        }
    }

    public List<Channel> LoadFavoriteChannels()
    {
        try
        {
            if (!_storageProvider.FileExists(FavoriteChannelsFile))
                return [];

            var json = _storageProvider.ReadText(FavoriteChannelsFile);
            return JsonSerializer.Deserialize<List<Channel>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void SaveFavoriteChannels(IEnumerable<Channel> channels)
    {
        var json = JsonSerializer.Serialize(channels.ToList());
        _storageProvider.WriteText(FavoriteChannelsFile, json);
        RefreshFavorites();
    }

    public void AddFavorite(Channel channel)
    {
        if (channel == null || string.IsNullOrWhiteSpace(channel.Url))
            return;

        if (IsFavorite(channel))
            return;

        var favorites = LoadFavoriteChannels();
        favorites.Add(channel);
        SaveFavoriteChannels(favorites);
    }

    public void RemoveFavorite(Channel channel)
    {
        if (channel == null || string.IsNullOrWhiteSpace(channel.Url))
            return;

        var favorites = LoadFavoriteChannels();
        favorites.RemoveAll(f => string.Equals(f.Url, channel.Url, StringComparison.OrdinalIgnoreCase));
        SaveFavoriteChannels(favorites);
    }

    public bool IsFavorite(Channel channel)
    {
        if (channel == null || string.IsNullOrWhiteSpace(channel.Url))
            return false;
        return _urlCache.Contains(channel.Url.Trim());
    }
}
