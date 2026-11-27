// Opt in so `?` emits "null" in the Copilot JSON schema; oblivious types are schema'd as non-null.
#nullable enable

namespace AndyTV.Watchlist.Models;

// Researched Daily/Weekend watchlist, and the model's response schema minus the [FromFeed] fields.
// Game start times can span multiple days (weekend).
public sealed class DailyWatchlist
{
    public List<WatchlistGame> BestWatches { get; init; } = [];

    public required WatchPlan WatchPlan { get; init; }
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
    public List<WatchSource> Sources { get; init; } = [];

    // ESPN's lines and team abbreviations (e.g. "BAL") for the game. Editions saved before these came from
    // ESPN hold the model's copies instead.
    [FromFeed]
    public Betting? Betting { get; init; }

    [FromFeed]
    public string? AwayTeamAbbr { get; init; }

    [FromFeed]
    public string? HomeTeamAbbr { get; init; }
}

public sealed record WatchSource(string Title, string Url);

// Any field may be null; the away/home order matches the "Away @ Home" (or "Away vs Home") matchup.
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
