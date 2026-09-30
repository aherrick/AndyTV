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
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.ServerUrl))
        {
            return sourceUrl;
        }

        var serverUrl = config.ServerUrl.TrimEnd('/');
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
        var config = localConfigService.Load();
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.ServerUrl))
        {
            return;
        }

        try
        {
            using var response = await HttpClient.PostAsync($"{config.ServerUrl.TrimEnd('/')}/stop", null);
        }
        catch
        {
        }
    }

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
            await Task.Delay(500);
        }
        return false;
    }
}
