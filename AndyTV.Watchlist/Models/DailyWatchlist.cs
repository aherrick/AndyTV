namespace AndyTV.Watchlist.Models;

// Shared JSON shape for the Daily and Weekend Watchlist emails. The date is the
// publication date (Friday for weekend); game start times can span multiple days.
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
    public List<WatchSource>? Sources { get; init; }
    public Betting? Betting { get; init; }
}

public sealed record WatchSource(string Title, string Url);

// Any field may be null; the away/home order matches the "Away @ Home" matchup.
public sealed record Betting(
    decimal? AwaySpread,
    decimal? HomeSpread,
    decimal? AwayMoneyline,
    decimal? HomeMoneyline,
    decimal? Total
);

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
