using System.Text;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public static class SportsGuideFormatter
{
    public static SportsPosts CreatePosts(
        IReadOnlyList<SportsEvent> events,
        AiSportsGuide guide,
        DateOnly targetDate
    )
    {
        var ranked = guide.RankedEvents.ConvertAll(
            rankedEvent => (Event: events[rankedEvent.EventId], Ranked: rankedEvent)
        );

        var best = new StringBuilder()
            .AppendLine("📺 AndyTV - BEST SPORTS TODAY")
            .AppendLine($"{targetDate:dddd, MMMM d}")
            .AppendLine();

        for (var index = 0; index < ranked.Count; index++)
        {
            var (sportsEvent, rankedEvent) = ranked[index];
            best.AppendLine(
                    $"{index + 1}. {Icon(sportsEvent.Sport)} {RankedMatchup(sportsEvent, rankedEvent)} - {sportsEvent.StartTimeEastern:h:mm tt} ET{Network(rankedEvent)}"
                )
                .AppendLine(rankedEvent.Reason.Trim())
                .AppendLine();
        }

        var timeline = new StringBuilder()
            .AppendLine("⏰ AndyTV - TODAY'S SPORTS TIMELINE")
            .AppendLine($"{targetDate:dddd, MMMM d}")
            .AppendLine();

        foreach (var (sportsEvent, rankedEvent) in ranked.OrderBy(item => item.Event.StartTimeEastern))
        {
            timeline.AppendLine(
                $"{TimeCol(sportsEvent.StartTimeEastern)} ET {Icon(sportsEvent.Sport)} {RankedMatchup(sportsEvent, rankedEvent)}{Network(rankedEvent)}"
            );
        }

        return new SportsPosts(
            best.ToString().TrimEnd(),
            timeline.ToString().TrimEnd(),
            $"{TopPicks(ranked)}\n\n🤖 AndyTV AI WATCH PLAN\n\n{guide.WatchPlan.Trim()}"
        );
    }

    private static string TopPicks(List<(SportsEvent Event, RankedEvent Ranked)> ranked)
    {
        var picks = new StringBuilder().AppendLine("⭐ TOP PICKS");

        void Add(string label, IEnumerable<(SportsEvent Event, RankedEvent Ranked)> source)
        {
            if (source.Cast<(SportsEvent Event, RankedEvent Ranked)?>().FirstOrDefault() is { } pick)
            {
                picks.AppendLine(
                    $"{label} {RankedMatchup(pick.Event, pick.Ranked)} - {pick.Event.StartTimeEastern:h:mm tt} ET"
                );
            }
        }

        Add("🔥 Best overall:", ranked);
        Add("🏈 Best football:", ranked.Where(item => item.Event.Sport == "Football"));
        Add("⚾ Best baseball:", ranked.Where(item => item.Event.Sport == "Baseball"));
        Add("🏒 Best hockey:", ranked.Where(item => item.Event.Sport == "Hockey"));
        Add("🏀 Best basketball:", ranked.Where(item => item.Event.Sport == "Basketball"));
        Add("⚽ Best soccer:", ranked.Where(item => item.Event.Sport == "Soccer"));
        Add("🌙 Best late-night:", ranked.Where(item => item.Event.StartTimeEastern.Hour >= 22));

        return picks.ToString().TrimEnd();
    }

    private static string Network(RankedEvent rankedEvent) =>
        string.IsNullOrWhiteSpace(rankedEvent.Network) ? "" : $" - {rankedEvent.Network.Trim()}";

    // Pad single-digit hours with a digit-width figure space (U+2007) so times align in X's proportional font.
    private static string TimeCol(DateTimeOffset time)
    {
        var hour12 = time.Hour % 12 == 0 ? 12 : time.Hour % 12;
        var pad = hour12 < 10 ? "\u2007" : "";
        return $"{pad}{time:h:mm tt}";
    }

    private static string RankedMatchup(SportsEvent sportsEvent, RankedEvent rankedEvent)
    {
        var away = rankedEvent.AwayRank is int ar ? $"#{ar} {sportsEvent.Away}" : sportsEvent.Away;
        var home = rankedEvent.HomeRank is int hr ? $"#{hr} {sportsEvent.Home}" : sportsEvent.Home;
        return $"{away} @ {home}";
    }

    private static string Icon(string sport) => sport switch
    {
        "Baseball" => "⚾",
        "Football" => "🏈",
        "Hockey" => "🏒",
        "Basketball" => "🏀",
        "Soccer" => "⚽",
        _ => "📺",
    };
}
