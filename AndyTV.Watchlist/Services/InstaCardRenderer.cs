using System.Globalization;
using System.Net;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public sealed record InstaCard(string Name, string Html);

public sealed class InstaCardRenderer
{
    private const string TopFooter = "THE BEST SPORTS • RANKED DAILY";
    private const string TimelineFooter = "YOUR DAY • IN WATCHING ORDER";
    private const string WatchFooter = "YOUR SPORTS DAY • PLANNED";

    private static readonly string BaseTemplate = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "instatemplates", "_base.html")
    );

    // The v1 banner inlined as a data URI, since Cloudflare renders raw HTML with no base URL.
    private static readonly string Header = BuildHeader();

    public IReadOnlyList<InstaCard> Render(
        IReadOnlyList<SportsEvent> events,
        AiSportsGuide guide,
        DateOnly targetDate
    )
    {
        var date = $"{targetDate:dddd} • {targetDate:MMM d}".ToUpper(CultureInfo.InvariantCulture);

        var ranked = guide
            .RankedEvents.Select(
                (rankedEvent, index) =>
                    (Rank: index + 1, Event: events[rankedEvent.EventId], Ranked: rankedEvent)
            )
            .ToList();

        var byTime = ranked.OrderBy(item => item.Event.StartTimeEastern).ToList();

        return
        [
            Compose(
                "01-top20-1-10.html",
                date,
                "🏆 TOP 20 TODAY <span class=\"page-chip\">1–10</span>",
                RankBody(ranked.Take(10)),
                "",
                TopFooter
            ),
            Compose(
                "02-top20-11-20.html",
                date,
                "🏆 TOP 20 TODAY <span class=\"page-chip\">11–20</span>",
                RankBody(ranked.Skip(10).Take(10)),
                "",
                TopFooter
            ),
            Compose(
                "03-timeline-1-10.html",
                date,
                "🕒 TOP 20 TIMELINE <span class=\"page-chip\">1–10</span>",
                TimelineBody(byTime.Take(10)),
                "",
                TimelineFooter
            ),
            Compose(
                "04-timeline-11-20.html",
                date,
                "🕒 TOP 20 TIMELINE <span class=\"page-chip\">11–20</span>",
                TimelineBody(byTime.Skip(10).Take(10)),
                "",
                TimelineFooter
            ),
            Compose(
                "05-watchlist.html",
                date,
                "🤖 AI WATCH PLAN",
                WatchBody(events, guide),
                WatchCallout(events, guide),
                WatchFooter
            ),
        ];
    }

    private static InstaCard Compose(
        string name,
        string date,
        string title,
        string body,
        string callout,
        string footerNote
    )
    {
        var html = BaseTemplate
            .Replace("{{HEADER}}", Header)
            .Replace("{{TITLE}}", title)
            .Replace("{{DATE}}", date)
            .Replace("{{BODY}}", body)
            .Replace("{{CALLOUT}}", callout)
            .Replace("{{FOOTERNOTE}}", footerNote);

        return new InstaCard(name, html);
    }

    private static string RankBody(
        IEnumerable<(int Rank, SportsEvent Event, RankedEvent Ranked)> rows
    )
    {
        var body = string.Concat(
            rows.Select(row =>
                $"""
                <div class="rank-row">
                  <div class="rank">{row.Rank}</div>
                  <div class="sport">{SportsFormat.Icon(row.Event.Sport)}</div>
                  <div class="game"><strong>{Enc(SportsFormat.RankedMatchup(row.Event, row.Ranked))}</strong><span>{Enc(row.Event.League)}</span></div>
                  <div class="time">{Time(row.Event)}</div>
                </div>
                """
            )
        );

        return $"<div class=\"card ranking\"><div class=\"ranks\">{body}</div></div>";
    }

    private static string TimelineBody(
        IEnumerable<(int Rank, SportsEvent Event, RankedEvent Ranked)> rows
    )
    {
        var body = string.Concat(
            rows.Select(row =>
                $"""
                <div class="timeline-row">
                  <div class="timeline-time">{Time(row.Event)}</div>
                  <div class="dot"></div>
                  <div class="timeline-main">
                    <div class="timeline-game">{SportsFormat.Icon(row.Event.Sport)} {Enc(SportsFormat.RankedMatchup(row.Event, row.Ranked))}</div>
                    <div class="timeline-meta">#{row.Rank} OVERALL • {Enc(row.Event.League)}</div>
                  </div>
                </div>
                """
            )
        );

        return $"<div class=\"card timeline-card\">{body}</div>";
    }

    private static string WatchBody(IReadOnlyList<SportsEvent> events, AiSportsGuide guide)
    {
        var steps = guide
            .WatchPlanSteps.Where(step => step.EventId >= 0 && step.EventId < events.Count)
            .Select(step =>
                (
                    Event: events[step.EventId],
                    step.Note,
                    Ranked: guide.RankedEvents.Find(rankedEvent =>
                        rankedEvent.EventId == step.EventId
                    )
                )
            )
            .OrderBy(item => item.Event.StartTimeEastern)
            .ToList();

        var body = string.Concat(
            steps.Select(step =>
                $"""
                <div class="watch-row">
                  <div class="watch-time">{Time(step.Event)}</div>
                  <div class="watch-icon">{SportsFormat.Icon(step.Event.Sport)}</div>
                  <div class="watch-copy"><strong>{Enc(SportsFormat.RankedMatchup(step.Event, step.Ranked))}</strong><span>{Enc(step.Note)}</span></div>
                </div>
                """
            )
        );

        return $"<div class=\"card watch-card\">{body}</div>";
    }

    private static string WatchCallout(IReadOnlyList<SportsEvent> events, AiSportsGuide guide)
    {
        if (guide.AnchorEventId is not int anchorId || anchorId < 0 || anchorId >= events.Count)
        {
            return "";
        }

        var anchor = events[anchorId];
        var anchorRank = guide.RankedEvents.Find(rankedEvent => rankedEvent.EventId == anchorId);
        return $"<div class=\"callout\">🔥 PRIME-TIME ANCHOR: {Enc(SportsFormat.RankedMatchup(anchor, anchorRank))} at {Time(anchor)} ET</div>";
    }

    private static string BuildHeader()
    {
        var bytes = File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "andytvwatchlist_header.png")
        );
        return $"<img class=\"banner\" src=\"data:image/png;base64,{Convert.ToBase64String(bytes)}\">";
    }

    private static string Time(SportsEvent sportsEvent) =>
        sportsEvent.StartTimeEastern.ToString("h:mm tt", CultureInfo.InvariantCulture);

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
