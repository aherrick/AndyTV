namespace AndyTV.Watchlist.Models;

public sealed class AiSportsGuide
{
    public List<RankedEvent> RankedEvents { get; init; } = [];

    public List<WatchPlanStep> WatchPlanSteps { get; init; } = [];

    public int? AnchorEventId { get; init; }
}

public sealed record WatchPlanStep(int EventId, string Note);

public sealed record RankedEvent(
    int EventId,
    string Reason,
    string? Network,
    int? HomeRank,
    int? AwayRank
);

public sealed record SportsPosts(string Post1, string Post2, string Post3);
