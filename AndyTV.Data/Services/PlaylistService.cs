using System.Diagnostics;
using System.Text.RegularExpressions;
using AndyTV.Data.Helpers;
using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class PlaylistService(IStorageProvider storage) : IPlaylistService
{
    private const string PlaylistsFileName = "playlists.json";
    private static readonly HttpClient _httpClient = new();

    public List<(Playlist Playlist, List<Channel> Channels)> PlaylistChannels { get; private set; } = [];

    public List<Channel> Channels { get; private set; } = [];

    // Curated US/UK lists match only playlists flagged for it, so TV-show/movie
    // playlists don't pollute them.
    public List<Channel> UsUkChannels { get; private set; } = [];

    public List<Playlist> LoadPlaylists() =>
        storage.ReadJson<List<Playlist>>(PlaylistsFileName) ?? [];

    public void SavePlaylists(List<Playlist> playlists) =>
        storage.WriteJson(PlaylistsFileName, playlists);

    public async Task RefreshChannelsAsync() =>
        SetChannels(await LoadChannelsAsync(LoadPlaylists()));

    public void SetChannels(List<(Playlist Playlist, List<Channel> Channels)> playlistChannels)
    {
        Channels = Dedup(playlistChannels);
        UsUkChannels = Dedup(playlistChannels.Where(x => x.Playlist.ShowInUsUk));
        PlaylistChannels = playlistChannels;

        static List<Channel> Dedup(
            IEnumerable<(Playlist Playlist, List<Channel> Channels)> source) =>
            [.. source.SelectMany(x => x.Channels).DistinctBy(c => c.Url, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<List<(Playlist Playlist, List<Channel> Channels)>> LoadChannelsAsync(
        List<Playlist> playlists
    )
    {
        // Playlists sharing a URL (different configs) download it once.
        var downloads = playlists
            .Select(p => p.Url)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(url => url, LoadPlaylistTextAsync, StringComparer.OrdinalIgnoreCase);

        return
        [
            .. await Task.WhenAll(
                playlists.Select(async p => (p, await LoadChannels(p, downloads[p.Url])))
            ),
        ];
    }

    private static async Task<List<Channel>> LoadChannels(Playlist playlist, Task<string> download)
    {
        try
        {
            var m3uText = await download;
            if (string.IsNullOrWhiteSpace(m3uText))
            {
                return [];
            }

            var parsed = M3UManager.M3UManager.ParseFromString(m3uText);
            var rename = NameRegex(playlist);
            var category = playlist.Name ?? "Playlist";
            var channels = new List<Channel>(parsed.Channels.Count);

            foreach (var item in parsed.Channels)
            {
                // Drop malformed entries at the source so a null title/URL can never reach the menu.
                if (string.IsNullOrWhiteSpace(item.MediaUrl) || string.IsNullOrWhiteSpace(item.Title))
                {
                    continue;
                }

                channels.Add(
                    new Channel
                    {
                        RawName = item.Title,
                        Name = rename?.Replace(item.Title, playlist.NameReplace) ?? item.Title,
                        Url = item.MediaUrl,
                        Group = item.GroupTitle,
                        LogoUrl = item.Logo,
                        Category = category,
                    }
                );
            }

            return channels;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PLAYLIST] Failed to load '{playlist.Name}': {ex.Message}");
            return [];
        }
    }

    // Built once per playlist; an invalid pattern leaves names unchanged.
    private static Regex NameRegex(Playlist playlist)
    {
        if (string.IsNullOrWhiteSpace(playlist.NameFind) || playlist.NameReplace is null)
        {
            return null;
        }

        try
        {
            return new Regex(playlist.NameFind);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    // Async so failures surface when awaited inside LoadChannels' try/catch.
    private static async Task<string> LoadPlaylistTextAsync(string source)
    {
        if (UrlHelper.IsValidUrl(source))
            return await _httpClient.GetStringAsync(source);

        if (File.Exists(source))
            return await File.ReadAllTextAsync(source);

        return string.Empty;
    }
}
