namespace AndyTV.Watchlist.Models;

public enum WatchlistKind
{
    Daily,
    Weekend,
}

// Each kind saves its own edition so the Friday runs cannot overwrite each other.
public static class WatchlistKindExtensions
{
    public static string EditionBlobName(this WatchlistKind kind) => $"site/{kind.ToString().ToLowerInvariant()}.json";

    // Weekend runs on Friday and covers Saturday + Sunday.
    public static DateOnly[] Days(this WatchlistKind kind, DateOnly runDate) =>
        kind == WatchlistKind.Weekend ? [runDate.AddDays(1), runDate.AddDays(2)] : [runDate];
}
