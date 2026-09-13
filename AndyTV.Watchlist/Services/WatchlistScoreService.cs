using System.Globalization;
using System.Net.Http.Json;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

// Reads latest.json and returns the watchlist entries that are live, with ESPN scores attached.
public sealed class WatchlistScoreService(HttpClient http, EspnScoreService scores)
{
    private const string Source = "https://andytvwatchlist.blob.core.windows.net/andytv-watchlist/latest.json";

    public async Task<List<Game>> GetAsync(CancellationToken cancellationToken = default)
    {
        var feed = (await http.GetFromJsonAsync<WatchlistSiteModel>(Source, cancellationToken))!;
        var games = feed.Top.Games
            .Select(x => new WatchlistGame(x.Rank, x.Sport, x.League, x.Matchup,
                DateTimeOffset.Parse(x.TimeIso, CultureInfo.InvariantCulture), x.Network, x.Reason)
            {
                AwayTeam = x.AwayTeam,
                HomeTeam = x.HomeTeam,
            })
            .ToList();

        await scores.EnrichAsync(games, cancellationToken);

        // Same shape the site already renders, with live scores overlaid; live games only.
        return feed.Top.Games
            .Zip(games, (entry, enriched) => entry with { Score = enriched.Score })
            .Where(x => x.Score?.State == "in")
            .ToList();
    }
}
