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
            Top: new TopTab(TopPicks(games), games.Select(ToGame).ToList()),
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

    // Ranked games for the Top tab; the client re-sorts these by time for the Timeline tab.
    private static Game ToGame(WatchlistGame game) =>
        new(
            game.Rank,
            SportsFormat.Icon(game.Sport),
            game.Matchup,
            SportsFormat.Time(game.StartTimeIso),
            Iso(game.StartTimeIso),
            Net(game),
            game.League,
            game.Reason.Trim()
        );

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
