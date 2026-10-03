using System.Text;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public static class SportsGuideFormatter
{
    public static SportsPosts CreatePosts(DailyWatchlist watchlist, WatchlistKind kind, DateOnly targetDate)
    {
        var games = watchlist.BestWatches;
        var period = SportsFormat.Period(kind).ToUpperInvariant();
        var dates = SportsFormat.Dates(kind, targetDate);

        var best = new StringBuilder()
            .AppendLine($"📺 AndyTV - BEST SPORTS {period}")
            .AppendLine(dates)
            .AppendLine();

        foreach (var game in games)
        {
            best.AppendLine(
                    $"{game.Rank}. {SportsFormat.Icon(game.Sport)} {game.Matchup} - {SportsFormat.Time(game.StartTimeIso, kind)}{Network(game)}"
                );
            if (SportsFormat.Odds(game.Betting) is { Length: > 0 } odds)
            {
                best.AppendLine(odds);
            }
            best.AppendLine(game.Reason.Trim()).AppendLine();
        }

        var timeline = new StringBuilder()
            .AppendLine($"⏰ AndyTV - {period}'S SPORTS TIMELINE")
            .AppendLine(dates)
            .AppendLine();

        foreach (var game in games.OrderBy(game => game.StartTimeIso))
        {
            timeline.AppendLine(
                $"{SportsFormat.Time(game.StartTimeIso, kind)} {SportsFormat.Icon(game.Sport)} {game.Matchup}{Network(game)}"
            );
        }

        return new SportsPosts(
            best.ToString().TrimEnd(),
            timeline.ToString().TrimEnd(),
            $"{TopPicks(games, kind)}\n\n🗺️ AndyTV WATCH PLAN\n\n{WatchPlan(watchlist, kind)}"
        );
    }

    private static string TopPicks(List<WatchlistGame> games, WatchlistKind kind)
    {
        var picks = new StringBuilder().AppendLine("⭐ TOP PICKS");

        foreach (var (icon, label, game) in SportsFormat.TopPicks(games))
        {
            picks.AppendLine($"{icon} {label}: {game.Matchup} - {SportsFormat.Time(game.StartTimeIso, kind)}");
        }

        return picks.ToString().TrimEnd();
    }

    private static string WatchPlan(DailyWatchlist watchlist, WatchlistKind kind)
    {
        var steps = SportsFormat.WatchPlanSteps(watchlist);

        if (steps.Count == 0)
        {
            return "";
        }

        var lines = new StringBuilder();
        var summary = watchlist.WatchPlan.Summary;

        if (!string.IsNullOrWhiteSpace(summary))
        {
            lines.AppendLine(summary.Trim()).AppendLine();
        }

        foreach (var step in steps)
        {
            lines.AppendLine($"{step.Icon} {SportsFormat.Time(step.Time, kind)} {step.Matchup} - {step.Instruction}")
                .AppendLine();
        }

        return lines.ToString().TrimEnd();
    }

    private static string Network(WatchlistGame game) =>
        string.IsNullOrWhiteSpace(game.Network) ? "" : $" - {game.Network.Trim()}";
}
