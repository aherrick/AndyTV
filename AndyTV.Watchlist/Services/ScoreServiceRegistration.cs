using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace AndyTV.Watchlist.Services;

public static class ScoreServiceRegistration
{
    public static IServiceCollection AddWatchlistScores(this IServiceCollection services)
    {
        services.AddRefitGeneratedClient<IEspnApi>()
            .ConfigureHttpClient(ConfigureEspnClient)
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
