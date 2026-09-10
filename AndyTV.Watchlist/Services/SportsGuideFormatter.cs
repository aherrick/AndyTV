using System.Text;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public static class SportsGuideFormatter
{
    public static SportsPosts CreatePosts(DailyWatchlist watchlist, DateOnly targetDate)
    {
        var games = watchlist.BestWatches.OrderBy(game => game.Rank).ToList();

        var best = new StringBuilder()
            .AppendLine("📺 AndyTV - BEST SPORTS TODAY")
            .AppendLine($"{targetDate:dddd, MMMM d}")
            .AppendLine();

        foreach (var game in games)
        {
            best.AppendLine(
                    $"{game.Rank}. {SportsFormat.Icon(game.Sport)} {game.Matchup} - {SportsFormat.Time(game.StartTimeIso)}{Network(game)}"
                )
                .AppendLine(game.Reason.Trim())
                .AppendLine();
        }

        var timeline = new StringBuilder()
            .AppendLine("⏰ AndyTV - TODAY'S SPORTS TIMELINE")
            .AppendLine($"{targetDate:dddd, MMMM d}")
            .AppendLine();

        foreach (var game in games.OrderBy(game => game.StartTimeIso))
        {
            timeline.AppendLine(
                $"{SportsFormat.Time(game.StartTimeIso)} {SportsFormat.Icon(game.Sport)} {game.Matchup}{Network(game)}"
            );
        }

        return new SportsPosts(
            best.ToString().TrimEnd(),
            timeline.ToString().TrimEnd(),
            $"{TopPicks(games)}\n\n🗺️ AndyTV WATCH PLAN\n\n{WatchPlan(watchlist)}"
        );
    }

    private static string TopPicks(List<WatchlistGame> games)
    {
        var picks = new StringBuilder().AppendLine("⭐ TOP PICKS");

        foreach (var (icon, label, game) in SportsFormat.TopPicks(games))
        {
            picks.AppendLine($"{icon} {label}: {game.Matchup} - {SportsFormat.Time(game.StartTimeIso)}");
        }

        return picks.ToString().TrimEnd();
    }

    private static string WatchPlan(DailyWatchlist watchlist)
    {
        var steps = SportsFormat.WatchPlanSteps(watchlist);

        if (steps.Count == 0)
        {
            return "";
        }

        var lines = new StringBuilder();
        var summary = watchlist.WatchPlan!.Summary;

        if (!string.IsNullOrWhiteSpace(summary))
        {
            lines.AppendLine(summary.Trim()).AppendLine();
        }

        foreach (var step in steps)
        {
            var matchup = step.Matchup.Length == 0 ? "" : $"{step.Matchup} - ";
            lines.AppendLine($"{step.Icon} {SportsFormat.Time(step.Time)} {matchup}{step.Instruction}");
        }

        return lines.ToString().TrimEnd();
    }

    private static string Network(WatchlistGame game) =>
        string.IsNullOrWhiteSpace(game.Network) ? "" : $" - {game.Network.Trim()}";
}
