namespace AndyTV.Watchlist.Models;

public enum WatchlistKind
{
    Daily,
    Weekend,
}

// Keep the email discriminator and output filename together. All readers and
// publishers use these mappings so the Friday feeds cannot overwrite each other.
public static class WatchlistKindExtensions
{
    public static string EmailSubject(this WatchlistKind kind) => kind switch
    {
        WatchlistKind.Daily => "Daily Watchlist",
        WatchlistKind.Weekend => "Weekend Watchlist",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string FeedFileName(this WatchlistKind kind) => kind switch
    {
        WatchlistKind.Daily => "latest.json",
        WatchlistKind.Weekend => "latest_weekend.json",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
