using System.Text;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public sealed class SportsGuideFormatter
{
    public SportsPosts CreatePosts(
        IReadOnlyList<SportsEvent> events,
        AiSportsGuide guide,
        DateOnly targetDate
    )
    {
        var ranked = guide.RankedEvents
            .Select(rankedEvent => (Event: events[rankedEvent.EventId], Ranked: rankedEvent))
            .ToList();

        var best = new StringBuilder()
            .AppendLine("📺 ANDY TV - BEST SPORTS TODAY")
            .AppendLine($"{targetDate:dddd, MMMM d}")
            .AppendLine();

        for (var index = 0; index < ranked.Count; index++)
        {
            var (sportsEvent, rankedEvent) = ranked[index];
            best.AppendLine(
                    $"{index + 1}. {Icon(sportsEvent.Sport)} {sportsEvent.Matchup} - {sportsEvent.StartTimeEastern:h:mm tt} ET{Network(rankedEvent)}"
                )
                .AppendLine(rankedEvent.Reason.Trim())
                .AppendLine();
        }

        var timeline = new StringBuilder()
            .AppendLine("⏰ ANDY TV - TODAY'S SPORTS TIMELINE")
            .AppendLine($"{targetDate:dddd, MMMM d}")
            .AppendLine();

        foreach (var (sportsEvent, rankedEvent) in ranked.OrderBy(item => item.Event.StartTimeEastern))
        {
            timeline.AppendLine(
                $"{sportsEvent.StartTimeEastern:h:mm tt} ET {Icon(sportsEvent.Sport)} {sportsEvent.Matchup}{Network(rankedEvent)}"
            );
        }

        return new SportsPosts(
            best.ToString().TrimEnd(),
            timeline.ToString().TrimEnd(),
            $"{TopPicks(ranked)}\n\n🤖 ANDY TV AI WATCH PLAN\n\n{guide.WatchPlan.Trim()}"
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
                    $"{label} {pick.Event.Matchup} - {pick.Event.StartTimeEastern:h:mm tt} ET"
                );
            }
        }

        Add("🔥 Best overall:", ranked);
        Add("🏈 Best football:", ranked.Where(item => item.Event.Sport == "Football"));
        Add("⚾ Best baseball:", ranked.Where(item => item.Event.Sport == "Baseball"));
        Add("🏒 Best hockey:", ranked.Where(item => item.Event.Sport == "Hockey"));
        Add("⚽ Best soccer:", ranked.Where(item => item.Event.Sport == "Soccer"));
        Add("🌙 Best late-night:", ranked.Where(item => item.Event.StartTimeEastern.Hour >= 22));

        return picks.ToString().TrimEnd();
    }

    private static string Network(RankedEvent rankedEvent) =>
        string.IsNullOrWhiteSpace(rankedEvent.Network) ? "" : $" - {rankedEvent.Network.Trim()}";

    private static string Icon(string sport) => sport switch
    {
        "Baseball" => "⚾",
        "Football" => "🏈",
        "Hockey" => "🏒",
        "Soccer" => "⚽",
        _ => "📺",
    };
}
