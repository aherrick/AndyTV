using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public class LocalPlaybackService(ILocalConfigService localConfigService) : ILocalPlaybackService
{
    private static readonly HttpClient HttpClient = new();

    public async Task<string> ResolvePlaybackUrl(string sourceUrl)
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
            await HttpClient.PostAsync($"{serverUrl}/start?url={Uri.EscapeDataString(sourceUrl)}&quality={quality}", null);
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

        try
        {
            await HttpClient.PostAsync($"{config.ServerUrl.TrimEnd('/')}/stop", null);
        }
        catch
        {
        }
    }
}