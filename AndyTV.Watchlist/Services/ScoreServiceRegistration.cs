using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;

namespace AndyTV.Watchlist.Services;

public static class ScoreServiceRegistration
{
    public static IServiceCollection AddWatchlistScores(this IServiceCollection services)
    {
        services.AddRefitGeneratedClient<IEspnApi>()
            .ConfigureHttpClient(ConfigureEspnClient)
            .AddHttpMessageHandler(sp => new EspnDiagnosticsHandler(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger("EspnDiagnostics")))
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });

        services.AddTransient<EspnScoreService>();
        services.AddHttpClient("watchlist-feed", client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton(provider => new WatchlistScoreService(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("watchlist-feed"),
            provider.GetRequiredService<EspnScoreService>()));
        return services;
    }

    // ESPN sits behind Akamai bot protection; a UA alone still 403s, so the full Chrome header
    // set (+ decompression on the handler) is required. Standardized so every ESPN call is identical.
    private static void ConfigureEspnClient(HttpClient client)
    {
        client.BaseAddress = new Uri("https://site.api.espn.com");
        client.Timeout = TimeSpan.FromSeconds(15);

        var headers = client.DefaultRequestHeaders;
        headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
        headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        headers.Add("Sec-Fetch-Dest", "document");
        headers.Add("Sec-Fetch-Mode", "navigate");
        headers.Add("Sec-Fetch-Site", "none");
        headers.Add("Sec-Fetch-User", "?1");
        headers.Add("Upgrade-Insecure-Requests", "1");
    }
}

// TEMP DIAGNOSTIC: logs ESPN's real status, Server header, and body snippet on any non-2xx so we can
// see whether a 403 is an Akamai edge/IP denial ("Access Denied ... Reference #...edgesuite.net") vs. another cause.
internal sealed class EspnDiagnosticsHandler(ILogger logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var server = response.Headers.TryGetValues("Server", out var s) ? string.Join(",", s) : "(none)";
            logger.LogWarning(
                "ESPN {Status} for {Url} | Server={Server} | Body: {Body}",
                (int)response.StatusCode, request.RequestUri, server,
                body.Length > 500 ? body[..500] : body);

            // Preserve the body so Refit's ApiException.Content stays intact.
            var replacement = new StringContent(body);
            foreach (var h in response.Content.Headers)
            {
                replacement.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }
            response.Content = replacement;
        }

        return response;
    }
}
