using System.Globalization;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

// Builds the render-ready view model the static site fetches as latest.json. All presentation
// logic (Eastern times, sport icons, top picks, watch plan) stays here so app.js just fills the template.
public static class WatchlistSiteBuilder
{
    public static WatchlistSiteModel Build(DailyWatchlist watchlist, WatchlistKind kind, DateOnly targetDate)
    {
        var games = watchlist.BestWatches;

        return new WatchlistSiteModel(
            Date: SportsFormat.Dates(kind, targetDate),
            Updated: $"Updated {EasternTimeZone.Now.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)} at {SportsFormat.Time(DateTimeOffset.UtcNow)}",
            Top: new TopTab(TopPicks(games), games.ConvertAll(ToSiteGame)),
            Plan: PlanTab(watchlist)
        );
    }

    // By start time, then rank (unranked last), then matchup A-Z; events without a time go last as TBD.
    // Ranked games keep their feed matchup, so an exact match tags them; a renamed one just shows no rank.
    public static List<ScheduleEvent> Schedule(List<SportsEvent> events, DailyWatchlist watchlist)
    {
        var ranks = watchlist.BestWatches.DistinctBy(g => g.Matchup).ToDictionary(g => g.Matchup, g => g.Rank);
        return
        [
            .. events
                .Select(e => (Event: e, Rank: ranks.TryGetValue(e.Matchup, out var rank) ? rank : (int?)null))
                .OrderBy(x => x.Event.StartTimeIso ?? DateTimeOffset.MaxValue)
                .ThenBy(x => x.Rank ?? int.MaxValue)
                .ThenBy(x => x.Event.Matchup, StringComparer.OrdinalIgnoreCase)
                .Select(x => new ScheduleEvent(
                    x.Event.StartTimeIso is { } start ? SportsFormat.TimeNoZone(start) : "TBD",
                    x.Event.StartTimeIso is { } iso ? Iso(iso) : "",
                    SportsFormat.Icon(x.Event.Sport),
                    x.Event.Sport,
                    x.Event.League,
                    x.Event.Matchup,
                    x.Event.Network?.Trim() ?? "",
                    x.Rank
                )),
        ];
    }

    private static List<TopPick> TopPicks(List<WatchlistGame> games) =>
        SportsFormat.TopPicks(games).ConvertAll(pick => new TopPick(
                pick.Icon,
                pick.Label,
                pick.Game.Matchup,
                SportsFormat.TimeNoZone(pick.Game.StartTimeIso),
                Iso(pick.Game.StartTimeIso)
            ));

    // Ranked games for the Top tab; the client re-sorts these by time for the Timeline tab.
    private static SiteGame ToSiteGame(WatchlistGame game) =>
        new(
            game.Rank,
            SportsFormat.Icon(game.Sport),
            game.Matchup,
            SportsFormat.TimeNoZone(game.StartTimeIso),
            Iso(game.StartTimeIso),
            game.Network?.Trim() ?? "",
            game.League,
            game.Reason.Trim(),
            game.Sport,
            game.Sources,
            SportsFormat.OddsParts(game.Betting, game.AwayTeamAbbr, game.HomeTeamAbbr)
        );

    private static PlanTab PlanTab(DailyWatchlist watchlist)
    {
        var steps = SportsFormat.WatchPlanSteps(watchlist).ConvertAll(step => new PlanStep(
                Iso(step.Time),
                SportsFormat.TimeNoZone(step.Time),
                step.Icon,
                step.Matchup,
                step.Instruction,
                step.Secondaries.ConvertAll(s => new PlanAlt(s.Icon, s.Matchup))
            ));

        return new PlanTab(watchlist.WatchPlan.Summary?.Trim() ?? "", steps);
    }

    // Machine-readable ISO timestamp for the client's <time datetime="...">.
    private static string Iso(DateTimeOffset value) =>
        value.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
}
