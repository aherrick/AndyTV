using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public static partial class SportsFormat
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
            "Golf" => "⛳",
            "Tennis" => "🎾",
            "MMA" => "🥊",
            _ => "📺",
        };

    // Zone-less time for the site, which already shows an "All times ET" header.
    public static string TimeNoZone(DateTimeOffset value) =>
        EasternTimeZone.Convert(value).ToString("h:mm tt", CultureInfo.InvariantCulture);

    public static string Time(DateTimeOffset value) => $"{TimeNoZone(value)} ET";

    // Machine-readable ISO timestamp for the site's <time datetime="...">.
    public static string Iso(DateTimeOffset value) =>
        value.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);

    // Weekend times need the day, e.g. "Sat 1:00 PM ET".
    public static string Time(DateTimeOffset value, WatchlistKind kind) =>
        kind == WatchlistKind.Weekend ? $"{Day(value)} {Time(value)}" : Time(value);

    public static string Day(DateTimeOffset value) =>
        EasternTimeZone.Convert(value).ToString("ddd", CultureInfo.InvariantCulture);

    // "Indiana @ Purdue" → "indiana-vs-purdue-20261122" (Eastern date); withTime adds "-1930" for doubleheaders.
    public static string Slug(string matchup, DateTimeOffset start, bool withTime = false)
    {
        var ascii = string.Concat(matchup.Replace("@", "vs").Normalize(NormalizationForm.FormD).Where(char.IsAscii));
        var name = NonSlugCharacters().Replace(ascii.ToLowerInvariant(), "-").Trim('-');
        return $"{name}-{EasternTimeZone.Convert(start).ToString(withTime ? "yyyyMMdd-HHmm" : "yyyyMMdd", CultureInfo.InvariantCulture)}";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharacters();

    public static string Period(WatchlistKind kind) => kind == WatchlistKind.Weekend ? "This Weekend" : "Today";

    // Feed event as one diagnostic line, e.g. "Thu 2026-10-08 7:30 PM ET | Football | NCAA | South Florida @ UTSA | ESPN | ML USF +210 / UTSA -260".
    public static string EventLine(SportsEvent e)
    {
        List<string> parts =
        [
            e.StartTimeIso is { } start
                ? $"{EasternTimeZone.Convert(start).ToString("ddd yyyy-MM-dd", CultureInfo.InvariantCulture)} {Time(start)}"
                : "TBD",
            e.Sport,
            e.League,
            e.Matchup,
        ];
        if (e.Note is not null)
        {
            parts.Add(e.Note);
        }
        if (e.Network is not null)
        {
            parts.Add(e.Network);
        }

        var odds = OddsParts(e.Betting, e.AwayAbbr, e.HomeAbbr);
        if (odds.Count > 0)
        {
            parts.Add(string.Join(" · ", odds));
        }
        return string.Join(" | ", parts);
    }

    // "Friday, October 3" or "Saturday, October 4 – Sunday, October 5".
    public static string Dates(WatchlistKind kind, DateOnly runDate, string format = "dddd, MMMM d") =>
        string.Join(" – ", kind.Days(runDate).Select(day => day.ToString(format, CultureInfo.InvariantCulture)));

    // e.g. "DAL -3.5 · ML NYG +150 / DAL -180 · O/U 47.5" or, for soccer, "Mainz -1.5 · ML Augsburg +475 / Mainz -220 · Draw +350 · O/U 3.5";
    // empty when there are no lines.
    public static string Odds(WatchlistGame game) => string.Join(" · ", OddsParts(game));

    public static List<string> OddsParts(WatchlistGame game) => OddsParts(game.Betting, game.AwayTeamAbbr, game.HomeTeamAbbr);

    // With both abbreviations: ["BAL -3.5", "ML BAL -180 / CIN +150", "O/U 52.5"].
    public static List<string> OddsParts(Betting betting, string awayAbbr = null, string homeAbbr = null)
    {
        List<string> parts = [];
        if (betting is null)
        {
            return parts;
        }

        // One spread implies the other side, so only the favorite's line is shown.
        if ((betting.HomeSpread ?? -betting.AwaySpread) is { } homeSpread)
        {
            const string spreadFormat = "+0.#;-0.#;PK";
            if (awayAbbr is { Length: > 0 } && homeAbbr is { Length: > 0 })
            {
                var (favorite, line) = homeSpread <= 0 ? (homeAbbr, homeSpread) : (awayAbbr, -homeSpread);
                parts.Add(line == 0 ? "PK" : $"{favorite} {Line(line, spreadFormat)}");
            }
            else
            {
                // Without abbreviations (editions saved before the feed kept them) only the away / home order says
                // who's favored, so both sides are shown.
                parts.Add($"Spread {Line(-homeSpread, spreadFormat)} / {Line(homeSpread, spreadFormat)}");
            }
        }
        // A moneyline can't be inferred from the other side, so show it only when both are present, each side
        // named when the abbreviations are known ("ML OTT +215 / CAR -265"), else away / home ("ML +215 / -265").
        if (betting is { AwayMoneyline: { } awayMoneyline, HomeMoneyline: { } homeMoneyline })
        {
            var (away, home) = awayAbbr is { Length: > 0 } && homeAbbr is { Length: > 0 } ? ($"{awayAbbr} ", $"{homeAbbr} ") : ("", "");
            parts.Add($"ML {away}{Line(awayMoneyline, "+0;-0")} / {home}{Line(homeMoneyline, "+0;-0")}");
        }
        // Soccer's third result; without it two plus-money sides ("ML +155 / +155") look like a mistake.
        if (betting.DrawMoneyline is { } draw)
        {
            parts.Add($"Draw {Line(draw, "+0;-0")}");
        }
        if (betting.Total is { } total)
        {
            parts.Add($"O/U {Line(total, "0.#")}");
        }

        return parts;
    }

    private static string Line(decimal value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    // Best-per-category picks, shared by the X post and the site's Top tab. Games arrive in rank order.
    public static List<(string Icon, string Label, WatchlistGame Game)> TopPicks(
        IReadOnlyList<WatchlistGame> games
    )
    {
        List<(string Icon, string Label, WatchlistGame Game)> picks = [];

        void Add(string icon, string label, IEnumerable<WatchlistGame> source)
        {
            // Skip a category whose top game is already shown (e.g. "Best football" == "Best overall").
            if (source.FirstOrDefault() is { } game && picks.TrueForAll(pick => pick.Game != game))
            {
                picks.Add((icon, label, game));
            }
        }

        Add("🔥", "Best overall", games);
        // Every sport in the list, ordered by its best-ranked game.
        foreach (var sport in games.GroupBy(game => game.Sport))
        {
            Add(Icon(sport.Key), $"Best {sport.Key.ToLowerInvariant()}", sport);
        }
        Add("🌙", "Best late-night", games.Where(game => EasternTimeZone.Convert(game.StartTimeIso).Hour >= 22));

        return picks;
    }

    // Watch-plan steps in time order with primary + secondary games resolved by rank; shared by X, site, and IG.
    // Steps or secondaries that reference an unknown rank are skipped.
    public static List<WatchPlanEntry> WatchPlanSteps(DailyWatchlist watchlist)
    {
        var byRank = watchlist.BestWatches.DistinctBy(game => game.Rank).ToDictionary(game => game.Rank);

        return
        [
            .. watchlist
                .WatchPlan.Steps.Where(step => byRank.ContainsKey(step.PrimaryRank))
                .OrderBy(step => step.StartTimeIso)
                .Select(step =>
                {
                    var primary = byRank[step.PrimaryRank];
                    return new WatchPlanEntry(
                        step.StartTimeIso,
                        Icon(primary.Sport),
                        primary.Matchup,
                        step.Instruction.Trim(),
                        [
                            .. step
                                .SecondaryRanks.Where(byRank.ContainsKey)
                                .Select(rank => (Icon(byRank[rank].Sport), byRank[rank].Matchup)),
                        ]
                    );
                }),
        ];
    }
}

public sealed record WatchPlanEntry(
    DateTimeOffset Time,
    string Icon,
    string Matchup,
    string Instruction,
    List<(string Icon, string Matchup)> Secondaries
);
