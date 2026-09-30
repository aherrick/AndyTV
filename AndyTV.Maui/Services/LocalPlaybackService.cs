using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public class LocalPlaybackService(ILocalConfigService localConfigService)
{
    // The server kills the previous stream and only answers once the new playlist exists (up to ~25s).
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(35) };

    // Always pops a fresh server stream (the server stops it once nobody is watching);
    // falls back to the source url when local playback is off or fails.
    public async Task<string> Start(string sourceUrl)
    {
        var config = localConfigService.Load();
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.ServerUrl))
        {
            return sourceUrl;
        }

        var serverUrl = config.ServerUrl.TrimEnd('/');
        var quality = string.IsNullOrWhiteSpace(config.Quality) ? "320" : config.Quality;

        try
        {
            using var response = await HttpClient.PostAsync(
                $"{serverUrl}/start?url={Uri.EscapeDataString(sourceUrl)}&quality={quality}",
                null
            );
            response.EnsureSuccessStatusCode();

            var id = (await response.Content.ReadAsStringAsync()).Trim();
            return $"{serverUrl}/{id}/live.m3u8";
        }
        catch
        {
            return sourceUrl;
        }
    }
}
