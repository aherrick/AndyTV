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
            "Golf" => "⛳",
            "Tennis" => "🎾",
            _ => "📺",
        };

    // Zone-less time for the site, which already shows an "All times ET" header.
    public static string TimeNoZone(DateTimeOffset value) =>
        EasternTimeZone.Convert(value).ToString("h:mm tt", CultureInfo.InvariantCulture);

    public static string Time(DateTimeOffset value) => $"{TimeNoZone(value)} ET";

    // e.g. "Spread +3.5 / -3.5 · ML +150 / -180 · O/U 47.5"; empty when there are no lines.
    public static string Odds(Betting? betting)
    {
        if (betting is null)
        {
            return "";
        }

        List<string> parts = [];
        // One spread implies the other side.
        if ((betting.AwaySpread ?? -betting.HomeSpread) is { } awaySpread)
        {
            parts.Add($"Spread {Line(awaySpread, "+0.#;-0.#;PK")} / {Line(betting.HomeSpread ?? -awaySpread, "+0.#;-0.#;PK")}");
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

        return string.Join(" · ", parts);
    }

    private static string Line(decimal value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    // Best-per-category picks, shared by the X post and the site's Top tab. Games arrive in rank order.
    public static List<(string Icon, string Label, WatchlistGame Game)> TopPicks(
        IReadOnlyList<WatchlistGame> games
    )
    {
        var picks = new List<(string Icon, string Label, WatchlistGame Game)>();

        void Add(string icon, string label, IEnumerable<WatchlistGame> source)
        {
            // Skip a category whose top game is already shown (e.g. "Best football" == "Best overall").
            if (source.FirstOrDefault() is { } game && picks.TrueForAll(pick => pick.Game != game))
            {
                picks.Add((icon, label, game));
            }
        }

        Add("🔥", "Best overall", games);
        Add("🏈", "Best football", games.Where(game => game.Sport == "Football"));
        Add("⚾", "Best baseball", games.Where(game => game.Sport == "Baseball"));
        Add("🏒", "Best hockey", games.Where(game => game.Sport == "Hockey"));
        Add("🏀", "Best basketball", games.Where(game => game.Sport == "Basketball"));
        Add("⚽", "Best soccer", games.Where(game => game.Sport == "Soccer"));
        Add("⛳", "Best golf", games.Where(game => game.Sport == "Golf"));
        Add(
            "🌙",
            "Best late-night",
            games.Where(game => EasternTimeZone.Convert(game.StartTimeIso).Hour >= 22)
        );

        return picks;
    }

    // Watch-plan steps in time order with primary + secondary games resolved by rank; shared by X, site, and IG.
    public static List<WatchPlanEntry> WatchPlanSteps(DailyWatchlist watchlist)
    {
        if (watchlist.WatchPlan is not { Steps.Count: > 0 } plan)
        {
            return [];
        }

        var byRank = watchlist.BestWatches.ToDictionary(game => game.Rank);

        (string Icon, string Matchup) Resolve(int rank) =>
            byRank.TryGetValue(rank, out var game) ? (Icon(game.Sport), game.Matchup) : ("📺", "");

        return plan
            .Steps.OrderBy(step => step.StartTimeIso)
            .Select(step =>
            {
                var (icon, matchup) = Resolve(step.PrimaryRank);
                var secondaries = step
                    .SecondaryRanks.Select(Resolve)
                    .Where(game => game.Matchup.Length > 0)
                    .ToList();
                return new WatchPlanEntry(
                    step.StartTimeIso,
                    icon,
                    matchup,
                    step.Instruction.Trim(),
                    secondaries
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
