using System.Globalization;
using System.Net;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public sealed record InstaCard(string Name, string Html);

public static class InstaCardRenderer
{
    private const string TopFooter = "THE BEST SPORTS • RANKED DAILY";
    private const string TimelineFooter = "YOUR DAY • IN WATCHING ORDER";
    private const string WatchFooter = "YOUR SPORTS DAY • PLANNED";

    private static readonly string BaseTemplate = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "assets", "templates", "insta", "_base.html")
    );

    // The v1 banner inlined as a data URI, since Cloudflare renders raw HTML with no base URL.
    private static readonly string Header = BuildHeader();

    public static IReadOnlyList<InstaCard> Render(DailyWatchlist watchlist, DateOnly targetDate)
    {
        var date = $"{targetDate:dddd} • {targetDate:MMM d}".ToUpper(CultureInfo.InvariantCulture);

        var games = watchlist.BestWatches.OrderBy(game => game.Rank).ToList();
        var byTime = games.OrderBy(game => game.StartTimeIso).ToList();

        return
        [
            Compose(
                "01-top20-1-10.html",
                date,
                "🏆 TOP 20 TODAY <span class=\"page-chip\">1–10</span>",
                RankBody(games.Take(10)),
                "",
                TopFooter
            ),
            Compose(
                "02-top20-11-20.html",
                date,
                "🏆 TOP 20 TODAY <span class=\"page-chip\">11–20</span>",
                RankBody(games.Skip(10).Take(10)),
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
                "🗺️ WATCH PLAN",
                WatchBody(watchlist),
                WatchCallout(watchlist),
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

    private static string RankBody(IEnumerable<WatchlistGame> games)
    {
        var body = string.Concat(
            games.Select(game =>
                $"""
                <div class="rank-row">
                  <div class="rank">{game.Rank}</div>
                  <div class="sport">{SportsFormat.Icon(game.Sport)}</div>
                  <div class="game"><strong>{Enc(game.Matchup)}</strong><span>{Enc(game.League)}</span></div>
                  <div class="time">{Time(game.StartTimeIso)}</div>
                </div>
                """
            )
        );

        return $"<div class=\"card ranking\"><div class=\"ranks\">{body}</div></div>";
    }

    private static string TimelineBody(IEnumerable<WatchlistGame> games)
    {
        var body = string.Concat(
            games.Select(game =>
                $"""
                <div class="timeline-row">
                  <div class="timeline-time">{Time(game.StartTimeIso)}</div>
                  <div class="dot"></div>
                  <div class="timeline-main">
                    <div class="timeline-game">{SportsFormat.Icon(game.Sport)} {Enc(game.Matchup)}</div>
                    <div class="timeline-meta">#{game.Rank} OVERALL • {Enc(game.League)}</div>
                  </div>
                </div>
                """
            )
        );

        return $"<div class=\"card timeline-card\">{body}</div>";
    }

    private static string WatchBody(DailyWatchlist watchlist)
    {
        if (watchlist.WatchPlan is not { Steps.Count: > 0 } plan)
        {
            return "<div class=\"card watch-card\"></div>";
        }

        var byRank = watchlist.BestWatches.ToDictionary(game => game.Rank);

        var body = string.Concat(
            plan.Steps.OrderBy(step => step.StartTimeIso)
                .Select(step =>
                {
                    var hasPrimary = byRank.TryGetValue(step.PrimaryRank, out var primary);
                    var icon = hasPrimary ? SportsFormat.Icon(primary!.Sport) : "📺";
                    var matchup = hasPrimary ? primary!.Matchup : "";
                    return $"""
                        <div class="watch-row">
                          <div class="watch-time">{Time(step.StartTimeIso)}</div>
                          <div class="watch-icon">{icon}</div>
                          <div class="watch-copy"><strong>{Enc(matchup)}</strong><span>{Enc(step.Instruction.Trim())}</span></div>
                        </div>
                        """;
                })
        );

        return $"<div class=\"card watch-card\">{body}</div>";
    }

    private static string WatchCallout(DailyWatchlist watchlist)
    {
        var summary = watchlist.WatchPlan?.Summary;
        return string.IsNullOrWhiteSpace(summary)
            ? ""
            : $"<div class=\"callout\">🔥 {Enc(summary.Trim())}</div>";
    }

    // Hosted URL keeps the HTML small so Cloudflare Browser Rendering doesn't 422 on a huge inline image.
    private const string HeaderImageUrl =
        "https://raw.githubusercontent.com/aherrick/AndyTV/refs/heads/main/AndyTV.Watchlist/assets/img/andytvwatchlist_header.png";

    private static string BuildHeader() => $"<img class=\"banner\" src=\"{HeaderImageUrl}\">";

    private static string Time(DateTimeOffset value) =>
        EasternTimeZone.Convert(value).ToString("h:mm tt", CultureInfo.InvariantCulture);

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
