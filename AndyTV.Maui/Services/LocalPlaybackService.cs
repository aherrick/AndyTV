using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public class LocalPlaybackService(ILocalConfigService localConfigService) : ILocalPlaybackService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };
    private string _currentSourceUrl;

    public async Task<string> ResolvePlaybackUrl(string sourceUrl)
    {
        var config = localConfigService.Load();
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.ServerUrl))
        {
            return sourceUrl;
        }

        var serverUrl = config.ServerUrl.TrimEnd('/');

        // If the same source URL is already streaming, skip restarting
        if (string.Equals(_currentSourceUrl, sourceUrl, StringComparison.OrdinalIgnoreCase))
        {
            return $"{serverUrl}/live.m3u8";
        }

        var quality = string.IsNullOrWhiteSpace(config.Quality) ? "320" : config.Quality;

        try
        {
            await HttpClient.PostAsync($"{serverUrl}/start?url={Uri.EscapeDataString(sourceUrl)}&quality={quality}", null);
            _currentSourceUrl = sourceUrl;
            return $"{serverUrl}/live.m3u8";
        }
        catch
        {
            return sourceUrl;
        }
    }

    public async Task StopPlayback()
    {
        var config = localConfigService.Load();
        if (string.IsNullOrWhiteSpace(config.ServerUrl))
        {
            return;
        }

        _currentSourceUrl = null;

        try
        {
            await HttpClient.PostAsync($"{config.ServerUrl.TrimEnd('/')}/stop", null);
        }
        catch
        {
        }
    }
}