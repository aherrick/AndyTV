using AndyTV.Watchlist.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndyTV.Watchlist.Services;

public static class ScoreServiceRegistration
{
    public static IServiceCollection AddWatchlistScores(this IServiceCollection services)
    {
        services.AddHttpClient<ApiSportsScoreService>();
        services.AddHttpClient("watchlist-feed", client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddSingleton(provider => new WatchlistScoreService(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("watchlist-feed"),
            provider.GetRequiredService<ApiSportsScoreService>()));
        return services;
    }
}
