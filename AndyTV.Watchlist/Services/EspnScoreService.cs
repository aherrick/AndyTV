using System.Globalization;
using AndyTV.Watchlist.Models;
using Raffinert.FuzzySharp;

namespace AndyTV.Watchlist.Services;

public sealed class EspnScoreService(IEspnApi espn)
{
    // League name (upper-cased) -> ESPN scoreboard path for the US team sports. Everything else
    // is treated as soccer and routed to ESPN's "all" board, which returns every competition.
    private static readonly Dictionary<string, (string Sport, string League, int? Groups)> Leagues = new()
    {
        ["NFL"] = ("football", "nfl", null),
        ["NCAAF"] = ("football", "college-football", 80),
        ["CFB"] = ("football", "college-football", 80),
        ["MLB"] = ("baseball", "mlb", null),
        ["NBA"] = ("basketball", "nba", null),
        ["WNBA"] = ("basketball", "wnba", null),
        ["NCAAM"] = ("basketball", "mens-college-basketball", null),
        ["NCAAW"] = ("basketball", "womens-college-basketball", null),
        ["NHL"] = ("hockey", "nhl", null),
    };

    public async Task EnrichAsync(IEnumerable<WatchlistGame> games, CancellationToken cancellationToken = default)
    {
        var routed = new List<(WatchlistGame Game, (string Sport, string League, int? Groups, string Dates) Key)>();
        foreach (var game in games)
        {
            game.Score = null;
            routed.Add((game, Route(game)));
        }

        // One request per unique league/date window, all in flight at once.
        var boards = routed
            .Select(x => x.Key)
            .Distinct()
            .ToDictionary(key => key, key => espn.GetScoreboardAsync(key.Sport, key.League, key.Dates, key.Groups, cancellationToken));
        await Task.WhenAll(boards.Values);

        foreach (var (game, key) in routed)
        {
            if (FindGame(game, boards[key].Result.Events) is { } match)
            {
                game.Score = new(match.Away?.Score, match.Home?.Score, match.Status.Type.State, match.Status.Type.ShortDetail, DateTimeOffset.UtcNow);
            }
        }
    }

    // Best fuzzy team match in either home/away orientation; watchlist and ESPN often disagree on which side is home.
    public static EspnEvent? FindGame(WatchlistGame game, IEnumerable<EspnEvent> events) =>
        events
            .Select(x => new { Game = x, Score = Math.Max(
                TeamScore(game.AwayTeam, x.Away?.Team) + TeamScore(game.HomeTeam, x.Home?.Team),
                TeamScore(game.AwayTeam, x.Home?.Team) + TeamScore(game.HomeTeam, x.Away?.Team)) })
            .Where(x => x.Score >= 160)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Game)
            .FirstOrDefault();

    private static double TeamScore(string? name, EspnTeam? team) => team is null || name is null ? 0
        : new[] { team.DisplayName, team.ShortDisplayName, team.Abbreviation }
            .Max(x => Fuzz.WeightedRatio(name.ToLowerInvariant(), (x ?? "").ToLowerInvariant()));

    private static (string Sport, string League, int? Groups, string Dates) Route(WatchlistGame game)
    {
        if (game.League is null || !Leagues.TryGetValue(game.League.Trim().ToUpperInvariant(), out var l))
        {
            l = ("soccer", "all", null);
        }

        // ESPN buckets a scoreboard day by US Eastern, which the feed's offset already reflects.
        return (l.Sport, l.League, l.Groups, game.StartTimeIso.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }
}
