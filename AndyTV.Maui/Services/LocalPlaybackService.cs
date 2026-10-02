using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public class LocalPlaybackService(ILocalConfigService localConfigService)
{
    // A new session id makes the server kill the previous stream and start a fresh one on the first playlist fetch.
    public string GetUrl(string sourceUrl)
    {
        var config = localConfigService.Load();
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.ServerUrl))
        {
            return sourceUrl;
        }

        var quality = string.IsNullOrWhiteSpace(config.Quality) ? "320" : config.Quality;
        return $"{config.ServerUrl.TrimEnd('/')}/live.m3u8?session={Guid.NewGuid():N}&quality={quality}&url={Uri.EscapeDataString(sourceUrl)}";
    }
}
