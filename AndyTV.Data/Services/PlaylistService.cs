using System.Text.Json;
using System.Text.RegularExpressions;
using AndyTV.Data.Helpers;
using AndyTV.Data.Models;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace AndyTV.Data.Services;

public class PlaylistService(IStorageProvider storage) : IPlaylistService
{
    private const string PlaylistsFileName = "playlists.json";
    private static readonly HttpClient _httpClient = new();

    // 2 retries, 15-second timeout per attempt; on failure returns empty string and the playlist is skipped
    private static readonly ResiliencePipeline<string> _downloadPipeline =
        new ResiliencePipelineBuilder<string>()
            .AddRetry(new RetryStrategyOptions<string>
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<string>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<TaskCanceledException>(),
            })
            .AddTimeout(TimeSpan.FromSeconds(15))
            .Build();

    // Cached channel data
    public List<(Playlist Playlist, List<Channel> Channels)> PlaylistChannels
    {
        get;
        private set;
    } = [];

    public List<Channel> Channels { get; private set; } = [];

    public List<Playlist> LoadPlaylists()
    {
        try
        {
            if (!storage.FileExists(PlaylistsFileName))
                return [];

            var json = storage.ReadText(PlaylistsFileName);
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
        storage.WriteText(PlaylistsFileName, json);
    }

    public async Task RefreshChannelsAsync()
    {
        try
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in RefreshChannelsAsync: {ex}");
            throw;
        }
    }

    public async Task<List<(Playlist Playlist, List<Channel> Channels)>> LoadChannelsAsync(
        List<Playlist> playlists
    )
    {
        var tasks = playlists.Select(async p =>
        {
            try
            {
                var m3uText = await LoadPlaylistTextAsync(p.Url);
                if (string.IsNullOrWhiteSpace(m3uText))
                {
                    return (p, new List<Channel>());
                }

                var parsed = M3UManager.M3UManager.ParseFromString(m3uText);
                var channels = new List<Channel>(parsed.Channels.Count);

                foreach (var item in parsed.Channels)
                {
                    var url = item.MediaUrl;
                    var rawName = item.Title;
                    var name = rawName;

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

                    if (!string.IsNullOrWhiteSpace(p.NameFind) && p.NameReplace != null)
                    {
                        try
                        {
                            name = Regex.Replace(name, p.NameFind, p.NameReplace);
                        }
                        catch
                        {
                            // Regex failed, use original name
                        }
                    }

                    channels.Add(
                        new Channel
                        {
                            RawName = rawName,
                            Name = name,
                            Url = url,
                            Group = item.GroupTitle,
                            LogoUrl = item.Logo,
                        }
                    );
                }

                return (p, channels);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PLAYLIST] Failed to load '{p.Name}': {ex.Message}"
                );
                return (p, new List<Channel>());
            }
        });

        return [.. await Task.WhenAll(tasks)];
    }

    private static async Task<string> LoadPlaylistTextAsync(string source)
    {
        if (UrlHelper.IsValidUrl(source))
        {
            try
            {
                return await _downloadPipeline.ExecuteAsync(
                    async ct => await _httpClient.GetStringAsync(source, ct)
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PLAYLIST] HTTP download failed after retries for '{source}': {ex.Message}"
                );
                return string.Empty;
            }
        }

        if (File.Exists(source))
            return await File.ReadAllTextAsync(source);

        return string.Empty;
    }
}