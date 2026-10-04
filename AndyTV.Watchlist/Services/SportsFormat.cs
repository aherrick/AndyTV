using System.Globalization;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public static class SportsFormat
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
            "MMA" => "🥊",
            _ => "📺",
        };

    // Zone-less time for the site, which already shows an "All times ET" header.
    public static string TimeNoZone(DateTimeOffset value) =>
        EasternTimeZone.Convert(value).ToString("h:mm tt", CultureInfo.InvariantCulture);

    public static string Time(DateTimeOffset value) => $"{TimeNoZone(value)} ET";

    // Weekend times need the day, e.g. "Sat 1:00 PM ET".
    public static string Time(DateTimeOffset value, WatchlistKind kind) =>
        kind == WatchlistKind.Weekend ? $"{Day(value)} {Time(value)}" : Time(value);

    public static string Day(DateTimeOffset value) =>
        EasternTimeZone.Convert(value).ToString("ddd", CultureInfo.InvariantCulture);

    public static string Period(WatchlistKind kind) => kind == WatchlistKind.Weekend ? "This Weekend" : "Today";

    // "Friday, October 3" or "Saturday, October 4 – Sunday, October 5".
    public static string Dates(WatchlistKind kind, DateOnly runDate, string format = "dddd, MMMM d") =>
        string.Join(" – ", kind.Days(runDate).Select(day => day.ToString(format, CultureInfo.InvariantCulture)));

    // e.g. "Spread +3.5 / -3.5 · ML +150 / -180 · O/U 47.5"; empty when there are no lines.
    public static string Odds(Betting? betting) => string.Join(" · ", OddsParts(betting));

    // With both abbreviations: ["BAL -3.5", "DAL +3.5", "ML -180 / +150", "O/U 52.5"].
    public static List<string> OddsParts(Betting? betting, string? awayAbbr = null, string? homeAbbr = null)
    {
        List<string> parts = [];
        if (betting is null)
        {
            return parts;
        }

        // One spread implies the other side.
        if ((betting.AwaySpread ?? -betting.HomeSpread) is { } awaySpread)
        {
            const string spreadFormat = "+0.#;-0.#;PK";
            var awayLine = Line(awaySpread, spreadFormat);
            var homeLine = Line(betting.HomeSpread ?? -awaySpread, spreadFormat);
            if (awayAbbr is { Length: > 0 } && homeAbbr is { Length: > 0 })
            {
                parts.Add($"{awayAbbr} {awayLine}");
                parts.Add($"{homeAbbr} {homeLine}");
            }
            else
            {
                parts.Add($"Spread {awayLine} / {homeLine}");
            }
        }
        // A moneyline can't be inferred from the other side, so show it only when both are present.
        if (betting is { AwayMoneyline: { } awayMoneyline, HomeMoneyline: { } homeMoneyline })
        {
            parts.Add($"ML {Line(awayMoneyline, "+0;-0")} / {Line(homeMoneyline, "+0;-0")}");
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
        foreach (var sport in games.Select(game => game.Sport).Distinct())
        {
            Add(Icon(sport), $"Best {sport.ToLowerInvariant()}", games.Where(game => game.Sport == sport));
        }
        Add("🌙", "Best late-night", games.Where(game => EasternTimeZone.Convert(game.StartTimeIso).Hour >= 22));

        return picks;
    }

    // Watch-plan steps in time order with primary + secondary games resolved by rank; shared by X, site, and IG.
    // Steps or secondaries that reference an unknown rank are skipped.
    public static List<WatchPlanEntry> WatchPlanSteps(DailyWatchlist watchlist)
    {
        var byRank = watchlist.BestWatches.DistinctBy(game => game.Rank).ToDictionary(game => game.Rank);

        return watchlist
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
            })
            .ToList();
    }
}

public sealed record WatchPlanEntry(
    DateTimeOffset Time,
    string Icon,
    string Matchup,
    string Instruction,
    List<(string Icon, string Matchup)> Secondaries
);
