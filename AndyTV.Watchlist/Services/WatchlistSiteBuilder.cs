using System.Globalization;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

// Builds the render-ready view model the static site fetches as latest.json. All presentation
// logic (Eastern times, sport icons, top picks, watch plan) stays here so app.js just fills the template.
public static class WatchlistSiteBuilder
{
    public static WatchlistSiteModel Build(DailyWatchlist watchlist, DateOnly targetDate)
    {
        var games = watchlist.BestWatches.OrderBy(game => game.Rank).ToList();

        return new WatchlistSiteModel(
            Date: targetDate.ToString("dddd, MMMM d", CultureInfo.InvariantCulture),
            Updated: $"Updated {targetDate.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)}",
            Top: new TopTab(TopPicks(games), TopGames(games)),
            Timeline: TimelineRows(games),
            Plan: PlanTab(watchlist)
        );
    }

    private static List<TopPick> TopPicks(List<WatchlistGame> games) =>
        SportsFormat
            .TopPicks(games)
            .Select(pick => new TopPick(
                pick.Icon,
                pick.Label,
                pick.Game.Matchup,
                SportsFormat.Time(pick.Game.StartTimeIso),
                Iso(pick.Game.StartTimeIso)
            ))
            .ToList();

    private static List<TopGame> TopGames(List<WatchlistGame> games) =>
        games
            .Select(game => new TopGame(
                game.Rank,
                SportsFormat.Icon(game.Sport),
                game.Matchup,
                SportsFormat.Time(game.StartTimeIso),
                Iso(game.StartTimeIso),
                Net(game),
                game.League,
                game.Reason.Trim()
            ))
            .ToList();

    private static List<TimelineRow> TimelineRows(List<WatchlistGame> games) =>
        games
            .OrderBy(game => game.StartTimeIso)
            .Select(game => new TimelineRow(
                Iso(game.StartTimeIso),
                SportsFormat.Time(game.StartTimeIso),
                SportsFormat.Icon(game.Sport),
                game.Matchup,
                game.Rank,
                game.League,
                Net(game)
            ))
            .ToList();

    private static PlanTab PlanTab(DailyWatchlist watchlist)
    {
        var steps = SportsFormat
            .WatchPlanSteps(watchlist)
            .Select(step => new PlanStep(
                Iso(step.Time),
                SportsFormat.Time(step.Time),
                step.Icon,
                step.Matchup,
                step.Instruction,
                step.Secondaries.Select(s => new PlanAlt(s.Icon, s.Matchup)).ToList()
            ))
            .ToList();

        return new PlanTab(watchlist.WatchPlan?.Summary?.Trim() ?? "", steps);
    }

    // Machine-readable ISO timestamp for the client's <time datetime="...">.
    private static string Iso(DateTimeOffset value) =>
        value.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

    private static string Net(WatchlistGame game) => game.Network?.Trim() ?? "";
}
