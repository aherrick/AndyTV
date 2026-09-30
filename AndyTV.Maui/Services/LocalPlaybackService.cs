using AndyTV.Data.Models;
using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public class LocalPlaybackService(ILocalConfigService localConfigService)
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly TimeSpan PlaylistTimeout = TimeSpan.FromSeconds(20);

    // Returns the local server's HLS url, or the source url when local playback is off or the server fails.
    public async Task<string> Start(string sourceUrl)
    {
        var config = localConfigService.Load();
        if (ServerUrl(config) is not { } serverUrl)
        {
            return sourceUrl;
        }

        var quality = string.IsNullOrWhiteSpace(config.Quality) ? "320" : config.Quality;
        var playlistUrl = $"{serverUrl}/live.m3u8";

        try
        {
            using var response = await HttpClient.PostAsync(
                $"{serverUrl}/start?url={Uri.EscapeDataString(sourceUrl)}&quality={quality}",
                null
            );
            response.EnsureSuccessStatusCode();

            return await WaitForPlaylist(playlistUrl) ? playlistUrl : sourceUrl;
        }
        catch
        {
            return sourceUrl;
        }
    }

    public async Task Stop()
    {
        if (ServerUrl(localConfigService.Load()) is not { } serverUrl)
        {
            return;
        }

        try
        {
            using var response = await HttpClient.PostAsync($"{serverUrl}/stop", null);
        }
        catch
        {
        }
    }

    private static string ServerUrl(LocalConfig config) =>
        config.Enabled && !string.IsNullOrWhiteSpace(config.ServerUrl) ? config.ServerUrl.TrimEnd('/') : null;

    // ffmpeg only writes the playlist after its first segment; playing earlier 404s until the stall monitor retries.
    private static async Task<bool> WaitForPlaylist(string playlistUrl)
    {
        var deadline = DateTime.UtcNow + PlaylistTimeout;
        while (DateTime.UtcNow < deadline)
        {
            using var response = await HttpClient.GetAsync(playlistUrl);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            await Task.Delay(250);
        }
        return false;
    }
}
