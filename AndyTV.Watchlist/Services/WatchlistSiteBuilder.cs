using System.Globalization;
using System.Net;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

// Renders the whole single-page site from the events + AiSportsGuide, filling the
// index template's Top / Timeline / Plan tab bodies.
public static class WatchlistSiteBuilder
{
    private static readonly string Template = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "site", "index.template.html")
    );

    public static string BuildHtml(
        IReadOnlyList<SportsEvent> events,
        AiSportsGuide guide,
        DateOnly targetDate
    )
    {
        var ranked = guide
            .RankedEvents.Select(
                (rankedEvent, index) =>
                    (Rank: index + 1, Event: events[rankedEvent.EventId], Ranked: rankedEvent)
            )
            .ToList();

        return Template
            .Replace(
                "{{DATE}}",
                Enc(targetDate.ToString("dddd, MMMM d", CultureInfo.InvariantCulture))
            )
            .Replace("{{TOP}}", TopBody(ranked))
            .Replace("{{TIMELINE}}", TimelineBody(ranked))
            .Replace("{{PLAN}}", PlanBody(events, guide));
    }

    private static string TimelineBody(
        List<(int Rank, SportsEvent Event, RankedEvent Ranked)> ranked
    )
    {
        var rows = string.Concat(
            ranked
                .OrderBy(item => item.Event.StartTimeEastern)
                .Select(row =>
                    $"""
                    <li>
                      <div class="row-time">{Enc(Time(row.Event))}</div>
                      <div>
                        <div class="row-game">{SportsFormat.Icon(row.Event.Sport)} {Enc(SportsFormat.RankedMatchup(row.Event, row.Ranked))}</div>
                        <div class="row-meta">#{row.Rank} overall · {Enc(row.Event.League)}</div>
                      </div>
                    </li>
                    """
                )
        );

        return $"<ul class=\"rows\">{rows}</ul>";
    }

    private static string TopBody(List<(int Rank, SportsEvent Event, RankedEvent Ranked)> ranked)
    {
        var rows = string.Concat(
            ranked.Select(row =>
                $"""
                <li class="game">
                  <div class="rank">{row.Rank}</div>
                  <div class="body">
                    <div class="title">{SportsFormat.Icon(row.Event.Sport)} {Enc(SportsFormat.RankedMatchup(row.Event, row.Ranked))}</div>
                    <div class="meta">{Enc(Time(row.Event))}{Network(row.Ranked)} · {Enc(row.Event.League)}</div>
                    <div class="desc">{Enc(row.Ranked.Reason.Trim())}</div>
                  </div>
                </li>
                """
            )
        );

        return $"{TopPicksBody(ranked)}<ol class=\"games\">{rows}</ol>";
    }

    private static string TopPicksBody(
        List<(int Rank, SportsEvent Event, RankedEvent Ranked)> ranked
    )
    {
        var picks = SportsFormat.TopPicks(ranked.ConvertAll(row => (row.Event, row.Ranked)));

        if (picks.Count == 0)
        {
            return "";
        }

        var items = string.Concat(
            picks.Select(pick =>
                $"<li>{pick.Icon} <strong>{Enc(pick.Label)}:</strong> {Enc(SportsFormat.RankedMatchup(pick.Event, pick.Ranked))} · {Enc(Time(pick.Event))}</li>"
            )
        );

        return $"<h3 class=\"summary-title\">⭐ Top Picks</h3><ul class=\"summary\">{items}</ul>";
    }

    private static string PlanBody(IReadOnlyList<SportsEvent> events, AiSportsGuide guide)
    {
        var steps = SportsFormat.WatchPlan(events, guide);

        var summary =
            steps.Count == 0
                ? ""
                : $"<h3 class=\"summary-title\">The Watch Plan</h3><ul class=\"summary\">{string.Concat(steps.Select(step => $"<li>{Enc(SportsFormat.PlanLine(step))}</li>"))}</ul>";

        return $"{Anchor(events, guide)}{summary}";
    }

    private static string Anchor(IReadOnlyList<SportsEvent> events, AiSportsGuide guide)
    {
        if (guide.AnchorEventId is not int anchorId || anchorId < 0 || anchorId >= events.Count)
        {
            return "";
        }

        var anchor = events[anchorId];
        var anchorRank = guide.RankedEvents.Find(rankedEvent => rankedEvent.EventId == anchorId);
        return $"<div class=\"anchor\">🔥 Prime-time anchor: {Enc(SportsFormat.RankedMatchup(anchor, anchorRank))} · {Enc(Time(anchor))}</div>";
    }

    private static string Network(RankedEvent rankedEvent) =>
        string.IsNullOrWhiteSpace(rankedEvent.Network)
            ? ""
            : $" · <span class=\"net\">{Enc(rankedEvent.Network.Trim())}</span>";

    private static string Time(SportsEvent sportsEvent) =>
        $"{sportsEvent.StartTimeEastern.ToString("h:mm tt", CultureInfo.InvariantCulture)} ET";

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
