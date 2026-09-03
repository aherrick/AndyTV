namespace AndyTV.Watchlist.Models;

public sealed class AiSportsGuide
{
    public List<RankedEvent> RankedEvents { get; init; } = [];

    public string WatchPlan { get; init; } = "";
}

public sealed record RankedEvent(int EventId, string Reason, string? Network);

public sealed record SportsPosts(string Post1, string Post2, string Post3);
