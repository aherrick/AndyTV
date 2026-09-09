using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public static class SportsFormat
{
    public static string Icon(string sport) =>
        sport switch
        {
            "Baseball" => "⚾",
            "Football" => "🏈",
            "Hockey" => "🏒",
            "Basketball" => "🏀",
            "Soccer" => "⚽",
            "Racing" => "🏁",
            _ => "📺",
        };

    public static string RankedMatchup(SportsEvent sportsEvent, RankedEvent? rankedEvent)
    {
        if (string.IsNullOrEmpty(sportsEvent.Away))
        {
            return sportsEvent.Home;
        }

        var away = rankedEvent?.AwayRank is int ar ? $"#{ar} {sportsEvent.Away}" : sportsEvent.Away;
        var home = rankedEvent?.HomeRank is int hr ? $"#{hr} {sportsEvent.Home}" : sportsEvent.Home;
        return $"{away} @ {home}";
    }

    // Ordered watch-plan steps (icon/time via callers) — the single source shared by X, site, and IG.
    public static List<(SportsEvent Event, RankedEvent? Ranked, string Note)> WatchPlan(
        IReadOnlyList<SportsEvent> events,
        AiSportsGuide guide
    ) =>
        guide
            .WatchPlanSteps.Where(step => step.EventId >= 0 && step.EventId < events.Count)
            .Select(step =>
                (
                    Event: events[step.EventId],
                    Ranked: guide.RankedEvents.Find(rankedEvent =>
                        rankedEvent.EventId == step.EventId
                    ),
                    Note: step.Note.Trim()
                )
            )
            .OrderBy(item => item.Event.StartTimeEastern)
            .ToList();

    // One watch-plan line, identical for the X post and the site's Play-by-Play.
    public static string PlanLine((SportsEvent Event, RankedEvent? Ranked, string Note) step) =>
        $"{Icon(step.Event.Sport)} {step.Event.StartTimeEastern:h:mm tt} ET {step.Note}";

    // Best-per-category picks, shared by the X post and the site's Top tab.
    public static List<(string Icon, string Label, SportsEvent Event, RankedEvent Ranked)> TopPicks(
        IReadOnlyList<(SportsEvent Event, RankedEvent Ranked)> ranked
    )
    {
        var picks = new List<(string Icon, string Label, SportsEvent Event, RankedEvent Ranked)>();

        void Add(
            string icon,
            string label,
            IEnumerable<(SportsEvent Event, RankedEvent Ranked)> source
        )
        {
            if (
                source.Cast<(SportsEvent Event, RankedEvent Ranked)?>().FirstOrDefault() is { } pick
            )
            {
                picks.Add((icon, label, pick.Event, pick.Ranked));
            }
        }

        Add("🔥", "Best overall", ranked);
        Add("🏈", "Best football", ranked.Where(item => item.Event.Sport == "Football"));
        Add("⚾", "Best baseball", ranked.Where(item => item.Event.Sport == "Baseball"));
        Add("🏒", "Best hockey", ranked.Where(item => item.Event.Sport == "Hockey"));
        Add("🏀", "Best basketball", ranked.Where(item => item.Event.Sport == "Basketball"));
        Add("⚽", "Best soccer", ranked.Where(item => item.Event.Sport == "Soccer"));
        Add("🏁", "Best racing", ranked.Where(item => item.Event.Sport == "Racing"));
        Add("🌙", "Best late-night", ranked.Where(item => item.Event.StartTimeEastern.Hour >= 22));

        return picks;
    }
}
