using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using AndyTV.Data.Helpers;
using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class PlaylistService(IStorageProvider storage)
{
    private const string PlaylistsFileName = "playlists.json";
    private static readonly HttpClient _httpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();
        // Browsers negotiate compression themselves.
        if (!OperatingSystem.IsBrowser() && handler.SupportsAutomaticDecompression)
        {
            handler.AutomaticDecompression = DecompressionMethods.All;
        }
        return new HttpClient(handler);
    }

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
        playlists = [.. playlists.Where(p => p.Enabled)];

        // Share the read and parse, but apply each playlist's settings independently.
        var sources = playlists
            .Select(p => p.Url)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(url => url, LoadSourceAsync, StringComparer.Ordinal);

        return
        [
            .. await Task.WhenAll(
                playlists.Select(async p => (p, await LoadChannels(p, sources[p.Url])))
            ),
        ];
    }

    private static async Task<List<Channel>> LoadChannels(
        Playlist playlist, Task<IReadOnlyList<M3uEntry>> source)
    {
        try
        {
            var parsed = await source.ConfigureAwait(false);
            var rename = NameRegex(playlist);
            var category = playlist.Name ?? "Playlist";
            var channels = new List<Channel>(parsed.Count);

            foreach (var item in parsed)
            {
                channels.Add(
                    new Channel
                    {
                        RawName = item.Title,
                        Name = rename?.Replace(item.Title, playlist.NameReplace) ?? item.Title,
                        Url = item.Url,
                        Group = item.Group,
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

    private static async Task<IReadOnlyList<M3uEntry>> LoadSourceAsync(string source)
    {
        if (UrlHelper.IsValidUrl(source))
        {
            using var response = await _httpClient
                .GetAsync(source, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return await M3uParser.ParseAsync(stream).ConfigureAwait(false);
        }

        if (File.Exists(source))
        {
            await using var stream = File.OpenRead(source);
            return await M3uParser.ParseAsync(stream).ConfigureAwait(false);
        }

        return [];
    }
}
