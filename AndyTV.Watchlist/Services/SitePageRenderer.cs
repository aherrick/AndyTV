using System.Globalization;
using System.Net;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

// Renders the whole site as one static index.html; app.js only switches panels and filters lists.
public static class SitePageRenderer
{
    private static readonly string Trophy = Svg("""<path d="M6 9H4.5a2.5 2.5 0 0 1 0-5H6"/><path d="M18 9h1.5a2.5 2.5 0 0 0 0-5H18"/><path d="M4 22h16"/><path d="M10 14.66V17c0 .55-.47.98-.97 1.21C7.85 18.75 7 20.24 7 22"/><path d="M14 14.66V17c0 .55.47.98.97 1.21C16.15 18.75 17 20.24 17 22"/><path d="M18 2H6v7a6 6 0 0 0 12 0V2Z"/>""");
    private static readonly string Clock = Svg("""<circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16.5 12"/>""");
    private static readonly string Tv = Svg("""<rect width="20" height="15" x="2" y="7" rx="2" ry="2"/><polyline points="17 2 12 7 7 2"/>""");
    private static readonly string Calendar = Svg("""<rect width="18" height="18" x="3" y="4" rx="2" ry="2"/><line x1="16" x2="16" y1="2" y2="6"/><line x1="8" x2="8" y1="2" y2="6"/><line x1="3" x2="21" y1="10" y2="10"/><path d="M8 14h.01"/><path d="M12 14h.01"/><path d="M16 14h.01"/><path d="M8 18h.01"/><path d="M12 18h.01"/><path d="M16 18h.01"/>""");
    private static readonly string Download = Svg("""<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" x2="12" y1="15" y2="3"/>""");
    private static readonly string Share = Svg("""<circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/><line x1="8.59" x2="15.42" y1="13.51" y2="17.49"/><line x1="15.41" x2="8.59" y1="6.51" y2="10.49"/>""");
    private static readonly string Sun = Svg("""<circle cx="12" cy="12" r="4"/><path d="M12 2v2"/><path d="M12 20v2"/><path d="m4.93 4.93 1.41 1.41"/><path d="m17.66 17.66 1.41 1.41"/><path d="M2 12h2"/><path d="M20 12h2"/><path d="m6.34 17.66-1.41 1.41"/><path d="m19.07 4.93-1.41 1.41"/>""", "icon-sun");
    private static readonly string Moon = Svg("""<path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/>""", "icon-moon");

    private static readonly string Script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "assets", "site", "app.js"));

    public static string Render(SiteEdition daily, SiteEdition weekend)
    {
        var games = daily.Watchlist.BestWatches;
        var schedule = Schedule(daily);
        var chips = Chips(games.ConvertAll(game => game.Sport));
        List<string> tabs = [Tab("top", "Top", Trophy), Tab("timeline", "Timeline", Clock), Tab("plan", "Plan", Tv)];
        List<string> panels =
        [
            Panel("top", chips + Picks(games, false) + Cards(games, false)),
            Panel("timeline:top", chips + Subtabs("timeline", "top") + Rows(ByTime(games).ConvertAll(ToRow), false)),
            Panel("timeline:all", Chips(schedule.ConvertAll(row => row.Sport)) + Subtabs("timeline", "all") + Rows(schedule, false)),
            Panel("plan", Plan(daily.Watchlist)),
        ];

        if (weekend is not null)
        {
            var weekendGames = weekend.Watchlist.BestWatches;
            var weekendSchedule = Schedule(weekend);
            var weekendChips = Chips(weekendGames.ConvertAll(game => game.Sport));
            var picks = Picks(weekendGames, true);
            tabs.Add(Tab("weekend", "Weekend", Calendar));
            panels.AddRange(
            [
                Panel("weekend:top", weekendChips + Subtabs("weekend", "top") + picks + Cards(weekendGames, true)),
                Panel("weekend:timeline", weekendChips + Subtabs("weekend", "timeline") + picks + Cards(ByTime(weekendGames), true)),
                Panel("weekend:all", Chips(weekendSchedule.ConvertAll(row => row.Sport)) + Subtabs("weekend", "all") + Rows(weekendSchedule, true)),
            ]);
        }

        var date = Swap(
            SportsFormat.Dates(WatchlistKind.Daily, daily.TargetDate),
            weekend is null ? null : SportsFormat.Dates(WatchlistKind.Weekend, weekend.TargetDate)
        );
        var updated = Swap(Updated(daily), weekend is null ? null : Updated(weekend));

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>AndyTV Watchlist — Best Sports Today</title>
            <link rel="canonical" href="https://andytv.today/">
            <link rel="icon" type="image/png" href="/img/icon-192.png">
            <link rel="apple-touch-icon" href="/img/apple-touch-icon.png">
            <link rel="manifest" href="/manifest.webmanifest">
            <meta name="mobile-web-app-capable" content="yes">
            <meta name="apple-mobile-web-app-capable" content="yes">
            <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent">
            <meta name="apple-mobile-web-app-title" content="AndyTV">
            <meta name="description" content="The best sports to watch today, ranked daily by AndyTV.">
            <meta name="theme-color" content="#0b0e13" media="(prefers-color-scheme: dark)">
            <meta name="theme-color" content="#f4f6fa" media="(prefers-color-scheme: light)">
            <script>document.documentElement.dataset.theme = localStorage.getItem("andytv-theme") || (matchMedia("(prefers-color-scheme: light)").matches ? "light" : "dark");</script>
            <meta property="og:type" content="website">
            <meta property="og:title" content="AndyTV Watchlist — Best Sports Today">
            <meta property="og:description" content="The best sports to watch today, ranked daily by AndyTV.">
            <meta property="og:url" content="https://andytv.today/">
            <meta property="og:image" content="https://andytv.today/img/andytvwatchlist_header3.png">
            <meta name="twitter:card" content="summary_large_image">
            <link rel="stylesheet" href="/styles.css">
            <script async src="https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-2972208898178828" crossorigin="anonymous"></script>
            </head>
            <body>
            <div class="top-controls">
            <button class="control-btn" id="install-btn" type="button" aria-label="Add to Home Screen" title="Add to Home Screen" hidden>{{Download}}</button>
            <button class="control-btn" id="share-btn" type="button" aria-label="Share" hidden>{{Share}}</button>
            <button class="control-btn" id="theme-toggle" type="button" aria-label="Toggle light or dark theme">{{Sun}}{{Moon}}</button>
            </div>
            <header class="site-header">
            <img class="banner" src="/img/andytvwatchlist_header3.png" alt="AndyTV Watchlist">
            <h1>{{date}}</h1>
            <p class="times-note">All times ET</p>
            </header>
            <nav class="tabs" aria-label="Watchlist sections">{{string.Concat(tabs)}}</nav>
            <main class="wrap">{{string.Concat(panels)}}</main>
            <footer class="site-footer">
            <p>Made with ❤️ by <a href="https://x.com/andrew_herrick" target="_blank" rel="noopener">@andrew_herrick</a> · <a href="https://x.com/andytvwatchlist" target="_blank" rel="noopener">@andytvwatchlist</a> · <a href="/privacy">Privacy</a></p>
            <p class="updated">{{updated}}</p>
            </footer>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/pwa-add-to-homescreen@4.4.0/dist/add-to-homescreen.min.css">
            <script defer src="https://cdn.jsdelivr.net/npm/pwa-add-to-homescreen@4.4.0/dist/add-to-homescreen.min.js"></script>
            <script>{{Script}}</script>
            <script src="https://cdn.jsdelivr.net/gh/aherrick/andy-fx@1.0.2/andy-fx.min.js"></script>
            <script>new AndyFX({ theme: 'auto', country: 'auto', intensity: 'subtle', fallback: 'none' }).start();</script>
            <script type="module" src="https://static.cloudflareinsights.com/beacon.min.js" data-cf-beacon='{"token": "0ddc91364f5746f4814895c9d169b965"}'></script>
            </body>
            </html>
            """;
    }

    // Header and footer text that differs on the Weekend tab; CSS shows one by the active tab.
    private static string Swap(string daily, string weekend) =>
        weekend is null
            ? E(daily)
            : $"""<span class="daily-only">{E(daily)}</span><span class="weekend-only">{E(weekend)}</span>""";

    private static string Tab(string tab, string label, string icon) =>
        $"""<button type="button" data-go="{tab}">{icon}{label}</button>""";

    // All panels start hidden; app.js activates the one for the URL, so the wrong tab never paints.
    private static string Panel(string key, string content) =>
        $"""<section class="panel" data-panel="{key}" hidden>{content}</section>""";

    private static string Subtabs(string tab, string active)
    {
        string[] views = tab == "weekend" ? ["top", "timeline", "all"] : ["top", "all"];
        var buttons = views.Select(view =>
            $"""<button type="button"{Active(view == active)} data-go="{tab}:{view}">{char.ToUpperInvariant(view[0])}{view[1..]}</button>""");
        return $"""<div class="subtabs">{string.Concat(buttons)}</div>""";
    }

    private static string Updated(SiteEdition edition) =>
        $"Updated {EasternTimeZone.Convert(edition.Updated).ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)} at {SportsFormat.Time(edition.Updated)}";

    // Every feed event by start time, then rank (unranked last), then matchup A-Z; events without a time go last.
    // Ranked games keep their feed matchup, so an exact match tags them; a renamed one just shows no rank.
    private static List<ScheduleRow> Schedule(SiteEdition edition)
    {
        var ranked = edition.Watchlist.BestWatches.DistinctBy(game => game.Matchup).ToDictionary(game => game.Matchup);
        return
        [
            .. edition.Events
                .Select(e =>
                {
                    var game = ranked.GetValueOrDefault(e.Matchup);
                    // The feed often lacks a network the ranked game has.
                    var network = string.IsNullOrWhiteSpace(e.Network) ? game?.Network : e.Network;
                    return new ScheduleRow(e.StartTimeIso, e.Sport, e.League, e.Matchup, network, game?.Rank);
                })
                .OrderBy(row => row.Start ?? DateTimeOffset.MaxValue)
                .ThenBy(row => row.Rank ?? int.MaxValue)
                .ThenBy(row => row.Matchup, StringComparer.OrdinalIgnoreCase),
        ];
    }

    // Sport chips for one list, most games first; omitted when the list has a single sport.
    private static string Chips(List<string> sports)
    {
        var groups = sports.GroupBy(sport => sport).OrderByDescending(group => group.Count()).ToList();
        if (groups.Count < 2)
        {
            return "";
        }

        var chips = groups.Select(group =>
            $"""<button type="button" data-filter="{E(group.Key)}"><span>{SportsFormat.Icon(group.Key)} {E(group.Key)}</span> <span class="count">{group.Count()}</span></button>""");
        return $"""<div class="sport-filter"><button type="button" class="active" data-filter="">All <span class="count">{sports.Count}</span></button>{string.Concat(chips)}</div>""";
    }

    private static string Picks(List<WatchlistGame> games, bool weekend)
    {
        var picks = SportsFormat.TopPicks(games);
        if (picks.Count == 0)
        {
            return "";
        }

        var items = picks.Select(pick =>
            $"""<li><span>{pick.Icon}</span> <strong>{E(pick.Label)}:</strong> <span>{E(pick.Game.Matchup)}</span> · {Time(pick.Game.StartTimeIso, weekend)}</li>""");
        return $"""<div class="top-picks"><h3 class="summary-title top-picks-title">⭐ Top Picks</h3><ul class="summary">{string.Concat(items)}</ul></div>""";
    }

    private static string Cards(List<WatchlistGame> games, bool weekend) =>
        $"""<ol class="games">{string.Concat(games.Select(game => Card(game, weekend)))}</ol>""";

    private static string Card(WatchlistGame game, bool weekend)
    {
        var network = string.IsNullOrWhiteSpace(game.Network) ? "" : $" · {Net(game.Network)}";
        var oddsParts = SportsFormat.OddsParts(game.Betting, game.AwayTeamAbbr, game.HomeTeamAbbr);
        var odds = oddsParts.Count == 0
            ? ""
            : $"""<div class="odds">{string.Concat(oddsParts.Select(odd => $"<span>{E(odd)}</span>"))}</div>""";

        // AI-researched links: only http(s) URLs are rendered.
        var links = (game.Sources ?? [])
            .Where(source => Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .Select(source => $"""<a href="{E(source.Url)}" target="_blank" rel="noopener">{E(source.Title)}</a>""")
            .ToList();
        var sources = links.Count == 0
            ? ""
            : $"""<details class="sources"><summary>Sources {links.Count}</summary><div class="source-links">{string.Concat(links)}</div></details>""";

        return $"""<li class="game" data-sport="{E(game.Sport)}"><div class="rank">{game.Rank}</div><div class="body"><div class="title">{E(game.Matchup)}</div><div class="meta">{Time(game.StartTimeIso, weekend)}{network} · {SportsFormat.Icon(game.Sport)} {E(game.League)}</div>{odds}<div class="desc">{E(game.Reason.Trim())}</div>{sources}</div></li>""";
    }

    private static string Rows(List<ScheduleRow> rows, bool weekend) =>
        $"""<ul class="rows">{string.Concat(rows.Select(row => Row(row, weekend)))}</ul>""";

    private static string Row(ScheduleRow row, bool weekend)
    {
        var day = weekend && row.Start is { } start ? $"""<div class="row-day">{SportsFormat.Day(start)}</div>""" : "";
        var rank = row.Rank is { } value ? $"#{value} overall · " : "";
        var network = string.IsNullOrWhiteSpace(row.Network) ? "" : $"{Net(row.Network)} · ";
        return $"""<li data-sport="{E(row.Sport)}"><div class="row-time">{day}{Time(row.Start)}</div><div><div class="row-game">{E(row.Matchup)}</div><div class="row-meta">{rank}{network}{SportsFormat.Icon(row.Sport)} {E(row.League)}</div></div></li>""";
    }

    private static string Plan(DailyWatchlist watchlist)
    {
        var summaryText = watchlist.WatchPlan.Summary?.Trim();
        var summary = string.IsNullOrEmpty(summaryText)
            ? ""
            : $"""<div class="plan-summary"><strong>Game Plan</strong><span>{E(summaryText)}</span></div>""";
        var steps = SportsFormat.WatchPlanSteps(watchlist);
        if (steps.Count == 0)
        {
            return summary;
        }

        var items = steps.Select(step =>
        {
            var alternates = step.Secondaries.Count == 0
                ? ""
                : $"""<div class="plan-alts"><strong>Also:</strong> <span>{E(AltText(step.Secondaries))}</span></div>""";
            return $"""<li><span>{step.Icon}</span> {Time(step.Time)} <span>{E($"{step.Matchup} — {step.Instruction}")}</span>{alternates}</li>""";
        });
        return $"""{summary}<div><h3 class="summary-title">The Watch Plan</h3><ul class="summary">{string.Concat(items)}</ul></div>""";
    }

    // Up to two alternates plus a "+N more" overflow.
    private static string AltText(List<(string Icon, string Matchup)> alternates)
    {
        var text = string.Join(" · ", alternates.Take(2).Select(alt => $"{alt.Icon} {alt.Matchup}"));
        return alternates.Count > 2 ? $"{text} · +{alternates.Count - 2} more" : text;
    }

    private static ScheduleRow ToRow(WatchlistGame game) =>
        new(game.StartTimeIso, game.Sport, game.League, game.Matchup, game.Network, game.Rank);

    private static List<WatchlistGame> ByTime(List<WatchlistGame> games) => [.. games.OrderBy(game => game.StartTimeIso)];

    // Eastern start time; weekend cards and picks lead with the weekday since they span two days.
    private static string Time(DateTimeOffset? value, bool withDay = false)
    {
        if (value is not { } start)
        {
            return "<time>TBD</time>";
        }

        var day = withDay ? $"{EasternTimeZone.Convert(start).ToString("dddd", CultureInfo.InvariantCulture)} · " : "";
        return $"""<time datetime="{SportsFormat.Iso(start)}">{day}{SportsFormat.TimeNoZone(start)}</time>""";
    }

    private static string Net(string network) => $"""<span class="net">{E(network.Trim())}</span>""";

    private static string Active(bool active) => active ? " class=\"active\"" : "";

    private static string Svg(string body, string cssClass = null) =>
        $"""<svg{(cssClass is null ? "" : $" class=\"{cssClass}\"")} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{body}</svg>""";

    private static string E(string value) => WebUtility.HtmlEncode(value);

    // One line in a Timeline list: a ranked game, or a feed event tagged with its rank when it has one.
    private sealed record ScheduleRow(DateTimeOffset? Start, string Sport, string League, string Matchup, string Network, int? Rank);
}
