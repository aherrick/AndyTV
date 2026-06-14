using System.Net.Http;
using AndyTV.Data.Services;

namespace AndyTV.Maui.Services;

public class LocalPlaybackService(ILocalConfigService localConfigService) : ILocalPlaybackService
{
    private static readonly HttpClient HttpClient = new();

    public async Task<string> ResolvePlaybackUrl(string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return sourceUrl;
        }

        var config = localConfigService.Load();
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.ServerUrl))
        {
            return sourceUrl;
        }

        if (!Uri.TryCreate(config.ServerUrl, UriKind.Absolute, out var serverUri))
        {
            return sourceUrl;
        }

        if (serverUri.Scheme != Uri.UriSchemeHttp && serverUri.Scheme != Uri.UriSchemeHttps)
        {
            return sourceUrl;
        }

        var quality = string.IsNullOrWhiteSpace(config.Quality) ? "320" : config.Quality;
        var startUri = BuildStartUri(serverUri, sourceUrl, quality);

        try
        {
            using var response = await HttpClient.PostAsync(startUri, null);
            if (!response.IsSuccessStatusCode)
            {
                return sourceUrl;
            }

            return new Uri(serverUri, "live.m3u8").ToString();
        }
        catch
        {
            return sourceUrl;
        }
    }

    private static Uri BuildStartUri(Uri serverUri, string sourceUrl, string quality)
    {
        var startUri = new Uri(serverUri, "start");
        var builder = new UriBuilder(startUri)
        {
            Query = $"url={Uri.EscapeDataString(sourceUrl)}&quality={Uri.EscapeDataString(quality)}",
        };

        return builder.Uri;
    }
}