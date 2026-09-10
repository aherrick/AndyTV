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
                    $"{index + 1}. {SportsFormat.Icon(sportsEvent.Sport)} {SportsFormat.RankedMatchup(sportsEvent, rankedEvent)} - {sportsEvent.StartTimeEastern:h:mm tt} ET{Network(rankedEvent)}"
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
                $"{sportsEvent.StartTimeEastern:h:mm tt} ET {SportsFormat.Icon(sportsEvent.Sport)} {SportsFormat.RankedMatchup(sportsEvent, rankedEvent)}{Network(rankedEvent)}"
            );
        }

        var plan = SportsFormat.WatchPlan(events, guide).Select(SportsFormat.PlanLine);

        return new SportsPosts(
            best.ToString().TrimEnd(),
            timeline.ToString().TrimEnd(),
            $"{TopPicks(ranked)}\n\n🤖 AndyTV AI WATCH PLAN\n\n{string.Join('\n', plan)}"
        );
    }

    private static string TopPicks(List<(SportsEvent Event, RankedEvent Ranked)> ranked)
    {
        var picks = new StringBuilder().AppendLine("⭐ TOP PICKS");

        foreach (var (icon, label, sportsEvent, rankedEvent) in SportsFormat.TopPicks(ranked))
        {
            picks.AppendLine(
                $"{icon} {label}: {SportsFormat.RankedMatchup(sportsEvent, rankedEvent)} - {sportsEvent.StartTimeEastern:h:mm tt} ET"
            );
        }

        return picks.ToString().TrimEnd();
    }

    private static string Network(RankedEvent rankedEvent) =>
        string.IsNullOrWhiteSpace(rankedEvent.Network) ? "" : $" - {rankedEvent.Network.Trim()}";
}
