namespace AndyTV.Watchlist.Models;

// Shape of the daily JSON emailed by the OpenAI scheduled task (subject "AndyTV Daily Watchlist JSON").
public sealed class DailyWatchlist
{
    public string? Date { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public string? Timezone { get; init; }

    public List<WatchlistGame> BestWatches { get; init; } = [];

    public WatchPlan? WatchPlan { get; init; }
}

public sealed record WatchlistGame(
    int Rank,
    string Sport,
    string League,
    string Matchup,
    DateTimeOffset StartTimeIso,
    string? Network,
    string Reason
)
{
    public string? AwayTeam { get; init; }
    public string? HomeTeam { get; init; }
    public GameScore? Score { get; set; }
    public List<WatchSource>? Sources { get; init; }
}

public sealed record WatchSource(string Title, string Url);

public sealed record GameScore(string? Away, string? Home, string State, string? Detail, DateTimeOffset? UpdatedAt = null);

public sealed class WatchPlan
{
    public string? Summary { get; init; }

    public List<WatchPlanStep> Steps { get; init; } = [];
}

public sealed record WatchPlanStep(
    DateTimeOffset StartTimeIso,
    int PrimaryRank,
    List<int> SecondaryRanks,
    string Instruction
);

public sealed record SportsPosts(string Post1, string Post2, string Post3);
