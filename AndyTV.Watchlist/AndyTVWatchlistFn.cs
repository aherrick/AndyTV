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

    // Dry run through the ChatGPT plan (ChatGptClient) instead of Copilot: Daily and Weekend run one after
    // another, and nothing is saved or published (the result is only logged).
    private const bool UseChatGpt = true;

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
        List<Func<Task>> runs = [];
        if (!RunOnStartup || ForceDaily)
        {
            runs.Add(() => publishingService.Publish(WatchlistKind.Daily, targetDate, UseChatGpt, cancellationToken));
        }

        if (RunOnStartup ? ForceWeekend : targetDate.DayOfWeek == DayOfWeek.Friday)
        {
            runs.Add(() => publishingService.Publish(WatchlistKind.Weekend, targetDate, UseChatGpt, cancellationToken));
        }

        // Either way, one run still finishes if the other fails, then the failure surfaces.
        // The page is rebuilt from whatever feeds are live, so a failed social post never leaves the site stale.
        // A local, so the compiler doesn't flag the unused branch of the constant as unreachable.
        var chatGpt = UseChatGpt;
        try
        {
            if (chatGpt)
            {
                // One after another, so ChatGPT plan usage isn't spent by two long researches at once.
                List<Exception> errors = [];
                foreach (var run in runs)
                {
                    try
                    {
                        await run();
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        errors.Add(ex);
                    }
                }

                if (errors.Count > 0)
                {
                    throw new AggregateException(errors);
                }
            }
            else
            {
                await Task.WhenAll(runs.Select(run => run()));
            }
        }
        finally
        {
            // A ChatGPT run is a dry run, so it leaves the published site alone.
            if (!chatGpt)
            {
                await sitePublishService.Publish(cancellationToken);
            }
        }
    }
}