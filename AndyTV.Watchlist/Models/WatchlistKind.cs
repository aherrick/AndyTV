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

    // Weekend runs on Friday and covers Saturday + Sunday.
    public static DateOnly[] Days(this WatchlistKind kind, DateOnly runDate) =>
        kind == WatchlistKind.Weekend ? [runDate.AddDays(1), runDate.AddDays(2)] : [runDate];
}
