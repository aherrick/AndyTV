using AndyTV.Watchlist.Models;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;

namespace AndyTV.Watchlist;

/// <summary>
/// Runs Daily every day and Weekend on Friday at 3:30 AM Eastern.
/// WatchlistPublishingService handles email, publishing, and Sunday cleanup.
/// </summary>
public sealed class AndyTVWatchlistFn(WatchlistPublishingService publishingService)
{
    // One daily run: publish Daily, then also publish Weekend on Fridays.
    // The service deletes the weekend feed first on Sundays, even without a daily email.
    [Function(nameof(AndyTVWatchlistFn))]
    public async Task Run(
        [TimerTrigger("0 30 7,8 * * *")] TimerInfo timer,
        CancellationToken cancellationToken
    )
    {
        // 07:30 UTC is 3:30 AM EDT; 08:30 UTC is 3:30 AM EST. Skip the other firing.
        var easternNow = EasternTimeZone.Now;
        if (easternNow.Hour != 3)
        {
            return;
        }

        var targetDate = DateOnly.FromDateTime(easternNow.DateTime);
        await publishingService.PublishAsync(WatchlistKind.Daily, targetDate, cancellationToken);

        // A missing Daily email simply returns from the service; still check Weekend.
        if (targetDate.DayOfWeek == DayOfWeek.Friday)
        {
            await publishingService.PublishAsync(WatchlistKind.Weekend, targetDate, cancellationToken);
        }
    }
}
