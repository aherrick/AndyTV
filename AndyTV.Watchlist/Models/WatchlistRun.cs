namespace AndyTV.Watchlist.Models;

// One run's outcome, saved as JSON to the private container and shown by AndyTVWatchlistRunsFn.
public sealed class WatchlistRun
{
    public DateTimeOffset Started { get; init; }
    public required string Kind { get; init; }
    public int Events { get; set; }
    public string? Duration { get; set; }
    public double Cost { get; set; }
    public bool Feed { get; set; }
    public string? XPostId { get; set; }
    public string? InstagramId { get; set; }
    public string? Error { get; set; }
}
