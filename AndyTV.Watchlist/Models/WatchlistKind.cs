namespace AndyTV.Watchlist.Models;

public enum WatchlistKind
{
    Daily,
    Weekend,
}

// Each kind publishes to its own feed so the Friday runs cannot overwrite each other.
public static class WatchlistKindExtensions
{
    public static string FeedFileName(this WatchlistKind kind) => kind switch
    {
        WatchlistKind.Daily => "latest.json",
        WatchlistKind.Weekend => "latest_weekend.json",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
