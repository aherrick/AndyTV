using System.Text.Json;
using System.Text.RegularExpressions;
using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class PlaylistService : IPlaylistService
{
    private const string PlaylistsFileName = "playlists.json";
    private static readonly HttpClient _httpClient = new();

    private readonly IStorageProvider _storage;

    // Cached channel data
    public List<(Playlist Playlist, List<Channel> Channels)> PlaylistChannels { get; private set; } = [];
    public List<Channel> Channels { get; private set; } = [];

    public PlaylistService(IStorageProvider storage)
    {
        _storage = storage;
    }

    public List<Playlist> LoadPlaylists()
    {
        try
        {
            if (!_storage.FileExists(PlaylistsFileName))
                return [];

            var json = _storage.ReadText(PlaylistsFileName);
            return JsonSerializer.Deserialize<List<Playlist>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void SavePlaylists(List<Playlist> playlists)
    {
        var json = JsonSerializer.Serialize(playlists);
        _storage.WriteText(PlaylistsFileName, json);
    }

    public async Task RefreshChannelsAsync()
    {
        var playlists = LoadPlaylists();
        PlaylistChannels = await LoadChannelsAsync(playlists);

        Channels =
        [
            .. PlaylistChannels
                .SelectMany(x => x.Channels)
                .GroupBy(c => c.Url, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First()),
        ];
    }

    public async Task<List<(Playlist Playlist, List<Channel> Channels)>> LoadChannelsAsync(List<Playlist> playlists)
    {
        var tasks = playlists.Select(async p =>
        {
            try
            {
                var m3uText = await _httpClient.GetStringAsync(p.Url);
                if (string.IsNullOrWhiteSpace(m3uText))
                {
                    return (p, new List<Channel>());
                }

                var parsed = M3UManager.M3UManager.ParseFromString(m3uText);
                var channels = new List<Channel>(parsed.Channels.Count);

                foreach (var item in parsed.Channels)
                {
                    var url = item.MediaUrl;

                    if (!string.IsNullOrWhiteSpace(p.UrlFind) && p.UrlReplace != null)
                    {
                        try
                        {
                            url = Regex.Replace(url, p.UrlFind, p.UrlReplace);
                        }
                        catch
                        {
                            // Regex failed, use original URL
                        }
                    }

                    channels.Add(new Channel
                    {
                        Name = item.Title,
                        Url = url,
                        Group = item.GroupTitle,
                    });
                }

                return (p, channels);
            }
            catch
            {
                return (p, new List<Channel>());
            }
        });

        return [.. await Task.WhenAll(tasks)];
    }
}
