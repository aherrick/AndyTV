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
        if (watchlist.WatchPlan is not { Steps.Count: > 0 } plan)
        {
            return "";
        }

        var byRank = watchlist.BestWatches.ToDictionary(game => game.Rank);
        var lines = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(plan.Summary))
        {
            lines.AppendLine(plan.Summary.Trim()).AppendLine();
        }

        foreach (var step in plan.Steps.OrderBy(step => step.StartTimeIso))
        {
            var hasPrimary = byRank.TryGetValue(step.PrimaryRank, out var primary);
            var icon = hasPrimary ? SportsFormat.Icon(primary!.Sport) : "📺";
            var matchup = hasPrimary ? $"{primary!.Matchup} - " : "";
            lines.AppendLine(
                $"{icon} {SportsFormat.Time(step.StartTimeIso)} {matchup}{step.Instruction.Trim()}"
            );
        }

        return lines.ToString().TrimEnd();
    }

    private static string Network(WatchlistGame game) =>
        string.IsNullOrWhiteSpace(game.Network) ? "" : $" - {game.Network.Trim()}";
}
