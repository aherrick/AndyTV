using System.Globalization;
using System.Net.Http.Json;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

// Reads latest.json and returns the live (in-progress) games with ESPN scores attached.
public sealed class WatchlistScoreService(HttpClient http, EspnScoreService scores)
{
    private const string Source = "https://andytvwatchlist.blob.core.windows.net/andytv-watchlist/latest.json";

    public async Task<LiveScoreFeed> GetAsync(CancellationToken cancellationToken = default)
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
        return new(DateTimeOffset.UtcNow, games
            .Where(x => x.Score?.State == "in")
            .Select(x => new LiveGame(x.Rank, x.Matchup, x.League, x.AwayTeam!, x.HomeTeam!, x.Score!))
            .ToList());
    }
}

public sealed record LiveScoreFeed(DateTimeOffset CheckedAt, List<LiveGame> Games);
public sealed record LiveGame(int Rank, string Matchup, string League, string AwayTeam, string HomeTeam, GameScore Score);
