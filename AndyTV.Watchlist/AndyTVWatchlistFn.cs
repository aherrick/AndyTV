using AndyTV.Watchlist.Models;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;

namespace AndyTV.Watchlist;

/// <summary>
/// Runs Daily every day and Weekend on Friday at 2:30 AM Eastern.
/// WatchlistPublishingService handles research, publishing, and Sunday cleanup.
/// </summary>
public sealed class AndyTVWatchlistFn(WatchlistPublishingService publishingService)
{
    // Debug only: flip either to true to run it once at startup. Keep both false when deploying.
    private const bool ForceDaily = false;

    private const bool ForceWeekend = false;
    private const bool RunOnStartup = ForceDaily || ForceWeekend;

    // One daily run: publish Daily, and on Fridays research Weekend at the same time.
    // The service deletes the weekend feed first on Sundays.
    [Function(nameof(AndyTVWatchlistFn))]
    public async Task Run(
        [TimerTrigger("0 30 6,7 * * *", RunOnStartup = RunOnStartup)] TimerInfo _,
        CancellationToken cancellationToken
    )
    {
        var easternNow = EasternTimeZone.Now;

        // 06:30 UTC is 2:30 AM EDT; 07:30 UTC is 2:30 AM EST. Skip the other firing.
        if (!RunOnStartup && easternNow.Hour != 2)
        {
            return;
        }

        var targetDate = DateOnly.FromDateTime(easternNow.DateTime);
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
        await Task.WhenAll(runs);
    }
}