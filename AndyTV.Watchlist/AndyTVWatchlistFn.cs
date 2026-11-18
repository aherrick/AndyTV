using AndyTV.Watchlist.Models;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;

namespace AndyTV.Watchlist;

/// <summary>
/// Runs Daily every day and Weekend on Friday at 3:00 AM Eastern, late enough for West Coast
/// tournament games to finish so the next round's matchups aren't TBD.
/// WatchlistPublishingService handles research and publishing.
/// </summary>
public sealed class AndyTVWatchlistFn(
    WatchlistPublishingService publishingService,
    SitePublishService sitePublishService
)
{
    // Debug only: flip either to true to run it once at startup. Keep both false when deploying.
    private const bool ForceDaily = false;

    private const bool ForceWeekend = false;
    private const bool RunOnStartup = ForceDaily || ForceWeekend;

    // Timer invocations never overlap, so a plain static is enough.
    private static bool _forcedRunDone;

    // One daily run: publish Daily, and on Fridays research Weekend at the same time.
    [Function(nameof(AndyTVWatchlistFn))]
    public async Task Run(
        [TimerTrigger("0 0 7,8 * * *", RunOnStartup = RunOnStartup)] TimerInfo _,
        CancellationToken cancellationToken
    )
    {
        var easternNow = EasternTimeZone.Now;

        // Forced runs happen once at startup; ignore later scheduled or catch-up firings.
        // Otherwise 07:00 UTC is 3 AM EDT and 08:00 UTC is 3 AM EST; skip the other.
        if (RunOnStartup ? _forcedRunDone : easternNow.Hour != 3)
        {
            return;
        }
        _forcedRunDone = true;

        var targetDate = EasternTimeZone.Date(easternNow);
        List<Task> runs = [];
        if (!RunOnStartup || ForceDaily)
        {
            runs.Add(publishingService.Publish(WatchlistKind.Daily, targetDate, cancellationToken));
        }

        if (RunOnStartup ? ForceWeekend : targetDate.DayOfWeek == DayOfWeek.Friday)
        {
            runs.Add(
                publishingService.Publish(WatchlistKind.Weekend, targetDate, cancellationToken)
            );
        }

        // WhenAll lets one run finish even if the other fails, then surfaces the failure.
        // The page is rebuilt from whatever feeds are live, so a failed social post never leaves the site stale.
        try
        {
            await Task.WhenAll(runs);
        }
        finally
        {
            await sitePublishService.Publish(cancellationToken);
        }
    }
}