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
        var top = daily.Model.Top;
        var ranked = top.Games.ConvertAll(ToRow);
        List<string> tabs = [Tab("top", "Top", Trophy), Tab("timeline", "Timeline", Clock), Tab("plan", "Plan", Tv)];
        List<string> panels =
        [
            Panel("top", Chips(ranked) + Picks(top, false) + Cards(top.Games, false)),
            Panel("timeline:top", Chips(ranked) + Subtabs("timeline", "top") + Rows(ByTime(top.Games).ConvertAll(ToRow), false)),
            Panel("timeline:all", Chips(daily.Schedule) + Subtabs("timeline", "all") + Rows(daily.Schedule, false)),
            Panel("plan", Plan(daily.Model.Plan)),
        ];

        if (weekend is not null)
        {
            var weekendTop = weekend.Model.Top;
            var weekendChips = Chips(weekendTop.Games.ConvertAll(ToRow));
            var picks = Picks(weekendTop, true);
            tabs.Add(Tab("weekend", "Weekend", Calendar));
            panels.AddRange(
            [
                Panel("weekend:top", weekendChips + Subtabs("weekend", "top") + picks + Cards(weekendTop.Games, true)),
                Panel("weekend:timeline", weekendChips + Subtabs("weekend", "timeline") + picks + Cards(ByTime(weekendTop.Games), true)),
                Panel("weekend:all", Chips(weekend.Schedule) + Subtabs("weekend", "all") + Rows(weekend.Schedule, true)),
            ]);
        }

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>AndyTV Watchlist — Best Sports Today</title>
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
            <meta name="twitter:title" content="AndyTV Watchlist — Best Sports Today">
            <meta name="twitter:description" content="The best sports to watch today, ranked daily by AndyTV.">
            <meta name="twitter:image" content="https://andytv.today/img/andytvwatchlist_header3.png">
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
            <h1>{{Swap(daily.Model.Date, weekend?.Model.Date)}}</h1>
            <p class="times-note">All times ET</p>
            </header>
            <nav class="tabs" aria-label="Watchlist sections">{{string.Concat(tabs)}}</nav>
            <main class="wrap">{{string.Concat(panels)}}</main>
            <footer class="site-footer">
            <p>Made with ❤️ by <a href="https://x.com/andrew_herrick" target="_blank" rel="noopener">@andrew_herrick</a> · <a href="https://x.com/andytvwatchlist" target="_blank" rel="noopener">@andytvwatchlist</a> · <a href="/privacy">Privacy</a></p>
            <p class="updated">{{Swap(daily.Model.Updated, weekend?.Model.Updated)}}</p>
            </footer>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/pwa-add-to-homescreen@4.4.0/dist/add-to-homescreen.min.css">
            <script defer src="https://cdn.jsdelivr.net/npm/pwa-add-to-homescreen@4.4.0/dist/add-to-homescreen.min.js"></script>
            <script>{{Script}}</script>
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
        $"""<section class="panel" data-panel="{key}">{content}</section>""";

    private static string Subtabs(string tab, string active)
    {
        string[] views = tab == "weekend" ? ["top", "timeline", "all"] : ["top", "all"];
        var buttons = views.Select(view =>
            $"""<button type="button"{Active(view == active)} data-go="{tab}:{view}">{char.ToUpperInvariant(view[0])}{view[1..]}</button>""");
        return $"""<div class="subtabs">{string.Concat(buttons)}</div>""";
    }

    // Sport chips for one list, most games first; omitted when the list has a single sport.
    private static string Chips(List<ScheduleEvent> items)
    {
        var sports = items.GroupBy(item => item.Sport).OrderByDescending(group => group.Count()).ToList();
        if (sports.Count < 2)
        {
            return "";
        }

        var chips = sports.Select(group =>
            $"""<button type="button" data-filter="{E(group.Key)}"><span>{group.First().Icon} {E(group.Key)}</span> <span class="count">{group.Count()}</span></button>""");
        return $"""<div class="sport-filter"><button type="button" class="active" data-filter="">All <span class="count">{items.Count}</span></button>{string.Concat(chips)}</div>""";
    }

    private static string Picks(TopTab top, bool weekend)
    {
        if (top.Picks.Count == 0)
        {
            return "";
        }

        var items = top.Picks.Select(pick =>
            $"""<li><span>{pick.Icon}</span> <strong>{E(pick.Label)}:</strong> <span>{E(pick.Matchup)}</span> · {Time(When(pick.Time, pick.TimeIso, weekend), pick.TimeIso)}</li>""");
        return $"""<div class="top-picks"><h3 class="summary-title top-picks-title">⭐ Top Picks</h3><ul class="summary">{string.Concat(items)}</ul></div>""";
    }

    private static string Cards(List<SiteGame> games, bool weekend) =>
        $"""<ol class="games">{string.Concat(games.Select(game => Card(game, weekend)))}</ol>""";

    private static string Card(SiteGame game, bool weekend)
    {
        var network = string.IsNullOrEmpty(game.Network) ? "" : $" · {Net(game.Network)}";
        var odds = game.Odds is { Count: > 0 }
            ? $"""<div class="odds">{string.Concat(game.Odds.Select(odd => $"<span>{E(odd)}</span>"))}</div>"""
            : "";

        // AI-researched links: only http(s) URLs are rendered.
        var links = (game.Sources ?? [])
            .Where(source => Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .Select(source => $"""<a href="{E(source.Url)}" target="_blank" rel="noopener">{E(source.Title)}</a>""")
            .ToList();
        var sources = links.Count == 0
            ? ""
            : $"""<details class="sources"><summary>Sources {links.Count}</summary><div class="source-links">{string.Concat(links)}</div></details>""";

        return $"""<li class="game" data-sport="{E(game.Sport)}"><div class="rank">{game.Rank}</div><div class="body"><div class="title">{E(game.Matchup)}</div><div class="meta">{Time(When(game.Time, game.TimeIso, weekend), game.TimeIso)}{network} · {game.Icon} {E(game.League)}</div>{odds}<div class="desc">{E(game.Reason)}</div>{sources}</div></li>""";
    }

    private static string Rows(List<ScheduleEvent> rows, bool weekend) =>
        $"""<ul class="rows">{string.Concat(rows.Select(row => Row(row, weekend)))}</ul>""";

    private static string Row(ScheduleEvent row, bool weekend)
    {
        var day = weekend && !string.IsNullOrEmpty(row.TimeIso) ? $"""<div class="row-day">{Eastern(row.TimeIso, "ddd")}</div>""" : "";
        var rank = row.Rank is { } value ? $"#{value} overall · " : "";
        var network = string.IsNullOrEmpty(row.Network) ? "" : $"{Net(row.Network)} · ";
        return $"""<li data-sport="{E(row.Sport)}"><div class="row-time">{day}{Time(row.Time, row.TimeIso)}</div><div><div class="row-game">{E(row.Matchup)}</div><div class="row-meta">{rank}{network}{row.Icon} {E(row.League)}</div></div></li>""";
    }

    private static string Plan(PlanTab plan)
    {
        var summary = string.IsNullOrEmpty(plan.Summary)
            ? ""
            : $"""<div class="plan-summary"><strong>Game Plan</strong><span>{E(plan.Summary)}</span></div>""";
        if (plan.Steps.Count == 0)
        {
            return summary;
        }

        var steps = plan.Steps.Select(step =>
        {
            var text = string.IsNullOrEmpty(step.Matchup) ? step.Instruction : $"{step.Matchup} — {step.Instruction}";
            var alternates = step.Alternates.Count == 0
                ? ""
                : $"""<div class="plan-alts"><strong>Also:</strong> <span>{E(AltText(step.Alternates))}</span></div>""";
            return $"""<li><span>{step.Icon}</span> {Time(step.Time, step.TimeIso)} <span>{E(text)}</span>{alternates}</li>""";
        });
        return $"""{summary}<div><h3 class="summary-title">The Watch Plan</h3><ul class="summary">{string.Concat(steps)}</ul></div>""";
    }

    // Up to two alternates plus a "+N more" overflow.
    private static string AltText(List<PlanAlt> alternates)
    {
        var text = string.Join(" · ", alternates.Take(2).Select(alt => $"{alt.Icon} {alt.Matchup}"));
        return alternates.Count > 2 ? $"{text} · +{alternates.Count - 2} more" : text;
    }

    private static ScheduleEvent ToRow(SiteGame game) =>
        new(game.Time, game.TimeIso, game.Icon, game.Sport, game.League, game.Matchup, game.Network, game.Rank);

    private static List<SiteGame> ByTime(List<SiteGame> games) =>
        [.. games.OrderBy(game => DateTimeOffset.Parse(game.TimeIso, CultureInfo.InvariantCulture))];

    // Weekend lists span two days, so their times lead with the weekday.
    private static string When(string time, string iso, bool weekend) =>
        weekend && !string.IsNullOrEmpty(iso) ? $"{Eastern(iso, "dddd")} · {time}" : time;

    private static string Eastern(string iso, string format) =>
        EasternTimeZone.Convert(DateTimeOffset.Parse(iso, CultureInfo.InvariantCulture)).ToString(format, CultureInfo.InvariantCulture);

    private static string Time(string text, string iso) =>
        string.IsNullOrEmpty(iso) ? $"<time>{E(text)}</time>" : $"""<time datetime="{iso}">{E(text)}</time>""";

    private static string Net(string network) => $"""<span class="net">{E(network)}</span>""";

    private static string Active(bool active) => active ? " class=\"active\"" : "";

    private static string Svg(string body, string cssClass = null) =>
        $"""<svg{(cssClass is null ? "" : $" class=\"{cssClass}\"")} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{body}</svg>""";

    private static string E(string value) => WebUtility.HtmlEncode(value);
}
