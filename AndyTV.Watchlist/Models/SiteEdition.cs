namespace AndyTV.Watchlist.Models;

// One saved edition (Daily or Weekend): the run's raw output, which SitePageRenderer formats on every render.
public sealed record SiteEdition(DateOnly TargetDate, DateTimeOffset Updated, DailyWatchlist Watchlist, List<SportsEvent> Events);
