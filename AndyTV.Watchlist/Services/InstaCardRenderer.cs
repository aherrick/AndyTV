using System.Globalization;
using System.Net;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public sealed record InstaCard(string Name, string Html);

public static class InstaCardRenderer
{
    private static readonly string BaseTemplate = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "assets", "templates", "insta", "_base.html")
    );

    // Hosted URL keeps the HTML small so Cloudflare Browser Rendering doesn't 422 on a huge inline image.
    private const string Header =
        "<img class=\"banner\" src=\"https://andytv.today/img/andytvwatchlist_header3.png\">";

    public static IReadOnlyList<InstaCard> Render(DailyWatchlist watchlist, WatchlistKind kind, DateOnly targetDate)
    {
        var weekend = kind == WatchlistKind.Weekend;
        var date = string.Join(
                " – ",
                kind.Days(targetDate).Select(day => $"{day.ToString(weekend ? "ddd" : "dddd", CultureInfo.InvariantCulture)} • {day:MMM d}")
            )
            .ToUpper(CultureInfo.InvariantCulture);
        var period = SportsFormat.Period(kind).ToUpperInvariant();
        var span = weekend ? "WEEKEND" : "DAY";
        var topFooter = $"THE BEST SPORTS • RANKED {(weekend ? "FOR THE WEEKEND" : "DAILY")}";
        var timelineFooter = $"YOUR {span} • IN WATCHING ORDER";

        var games = watchlist.BestWatches;
        var byTime = games.OrderBy(game => game.StartTimeIso).ToList();

        return
        [
            Compose(
                "01-top20-1-10.html",
                date,
                $"🏆 TOP 20 {period} {Chip("1–10")}",
                RankBody(games.Take(10), kind),
                "",
                topFooter
            ),
            Compose(
                "02-top20-11-20.html",
                date,
                $"🏆 TOP 20 {period} {Chip("11–20")}",
                RankBody(games.Skip(10).Take(10), kind),
                "",
                topFooter
            ),
            Compose(
                "03-timeline-1-10.html",
                date,
                $"🕒 TOP 20 TIMELINE {Chip("1–10")}",
                TimelineBody(byTime.Take(10), kind),
                "",
                timelineFooter
            ),
            Compose(
                "04-timeline-11-20.html",
                date,
                $"🕒 TOP 20 TIMELINE {Chip("11–20")}",
                TimelineBody(byTime.Skip(10).Take(10), kind),
                "",
                timelineFooter
            ),
            Compose(
                "05-watchlist.html",
                date,
                "🗺️ WATCH PLAN",
                WatchBody(watchlist, kind),
                WatchCallout(watchlist),
                $"YOUR SPORTS {span} • PLANNED"
            ),
        ];
    }

    private static string Chip(string range) => $"<span class=\"page-chip\">{range}</span>";

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

    private static string RankBody(IEnumerable<WatchlistGame> games, WatchlistKind kind)
    {
        var body = string.Concat(
            games.Select(game =>
                $"""
                <div class="rank-row">
                  <div class="rank">{game.Rank}</div>
                  <div class="sport">{SportsFormat.Icon(game.Sport)}</div>
                  <div class="game"><strong>{Enc(game.Matchup)}</strong><span>{Enc(
                    LeagueAndOdds(game)
                )}</span></div>
                  <div class="time">{Time(game.StartTimeIso, kind)}</div>
                </div>
                """
            )
        );

        return $"<div class=\"card ranking\"><div class=\"ranks\">{body}</div></div>";
    }

    private static string LeagueAndOdds(WatchlistGame game) =>
        SportsFormat.Odds(game.Betting) is { Length: > 0 } odds
            ? $"{game.League} • {odds}"
            : game.League;

    private static string TimelineBody(IEnumerable<WatchlistGame> games, WatchlistKind kind)
    {
        var body = string.Concat(
            games.Select(game =>
                $"""
                <div class="timeline-row">
                  <div class="timeline-time">{Time(game.StartTimeIso, kind)}</div>
                  <div class="dot"></div>
                  <div class="timeline-main">
                    <div class="timeline-game">{SportsFormat.Icon(game.Sport)} {Enc(
                    game.Matchup
                )}</div>
                    <div class="timeline-meta">#{game.Rank} OVERALL • {Enc(game.League)}{Network(
                    game
                )}</div>
                  </div>
                </div>
                """
            )
        );

        return $"<div class=\"card timeline-card\">{body}</div>";
    }

    private static string WatchBody(DailyWatchlist watchlist, WatchlistKind kind)
    {
        var body = string.Concat(
            SportsFormat
                .WatchPlanSteps(watchlist)
                .Select(step =>
                    $"""
                    <div class="watch-row">
                      <div class="watch-time">{Time(step.Time, kind)}</div>
                      <div class="watch-icon">{step.Icon}</div>
                      <div class="watch-copy"><strong>{Enc(step.Matchup)}</strong><span>{Enc(
                        step.Instruction
                    )}</span></div>
                    </div>
                    """
                )
        );

        return $"<div class=\"card watch-card\">{body}</div>";
    }

    private static string WatchCallout(DailyWatchlist watchlist)
    {
        var summary = watchlist.WatchPlan.Summary;
        return string.IsNullOrWhiteSpace(summary)
            ? ""
            : $"<div class=\"callout\">🔥 {Enc(summary.Trim())}</div>";
    }

    // Weekend stacks a small day label above the time so the pill keeps its width.
    private static string Time(DateTimeOffset value, WatchlistKind kind) =>
        kind == WatchlistKind.Weekend
            ? $"<span class=\"day\">{SportsFormat.Day(value).ToUpperInvariant()}</span>{SportsFormat.TimeNoZone(value)}"
            : SportsFormat.TimeNoZone(value);

    private static string Network(WatchlistGame game) =>
        string.IsNullOrWhiteSpace(game.Network)
            ? ""
            : $" \u2022 <span class=\"net\">{Enc(game.Network.Trim())}</span>";

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}