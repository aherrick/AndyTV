namespace AndyTV.Watchlist.Models;

public enum WatchlistKind
{
    Daily,
    Weekend,
}

// One blob per kind and date (site/daily/2026-11-10.json); together they're the site's archive.
public static class WatchlistKindExtensions
{
    public static string EditionPrefix(this WatchlistKind kind) => $"site/{kind.ToString().ToLowerInvariant()}/";

    public static string EditionBlobName(this WatchlistKind kind, DateOnly date) => $"{kind.EditionPrefix()}{date:yyyy-MM-dd}.json";

    // Weekend runs on Friday and covers Saturday + Sunday.
    public static DateOnly[] Days(this WatchlistKind kind, DateOnly runDate) =>
        kind == WatchlistKind.Weekend ? [runDate.AddDays(1), runDate.AddDays(2)] : [runDate];
}
