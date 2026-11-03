using System.Text.Json;
using AndyTV.Watchlist.Models;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist;

/// <summary>
/// Runs Daily every day and Weekend on Friday at 2:30 AM Eastern.
/// WatchlistPublishingService handles research, publishing, and Sunday cleanup.
/// </summary>
public sealed class AndyTVWatchlistFn(
    WatchlistPublishingService publishingService,
    SitePublishService sitePublishService,
    WatchlistResearchService researchService,
    ILogger<AndyTVWatchlistFn> logger
)
{
    // Debug only: flip either to true to run it once at startup. Keep both false when deploying.
    private const bool ForceDaily = false;
    private const bool ForceWeekend = false;
    private const bool RunOnStartup = ForceDaily || ForceWeekend;

    // Debug only: research and log the result without touching blob storage, the site, Instagram or X.
    private const bool ResearchOnly = false;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // Timer invocations never overlap, so a plain static is enough.
    private static bool _forcedRunDone;

    // One daily run: publish Daily, and on Fridays research Weekend at the same time.
    // The service deletes the weekend feed first on Sundays.
    [Function(nameof(AndyTVWatchlistFn))]
    public async Task Run(
        [TimerTrigger("0 30 6,7 * * *", RunOnStartup = RunOnStartup)] TimerInfo _,
        CancellationToken cancellationToken
    )
    {
        var easternNow = EasternTimeZone.Now;

        // Forced runs happen once at startup; ignore later scheduled or catch-up firings.
        // Otherwise 06:30 UTC is 2:30 AM EDT and 07:30 UTC is 2:30 AM EST; skip the other.
        if (RunOnStartup ? _forcedRunDone : easternNow.Hour != 2)
        {
            return;
        }
        _forcedRunDone = true;

        var targetDate = EasternTimeZone.Date(easternNow);
        List<WatchlistKind> kinds = [];
        if (!RunOnStartup || ForceDaily)
        {
            kinds.Add(WatchlistKind.Daily);
        }

        if (RunOnStartup ? ForceWeekend : targetDate.DayOfWeek == DayOfWeek.Friday)
        {
            kinds.Add(WatchlistKind.Weekend);
        }

        // WhenAll lets one run finish even if the other fails, then surfaces the failure.
        // The page is rebuilt from whatever feeds are live, so a failed social post never leaves the site stale.
        try
        {
            await Task.WhenAll(kinds.Select(kind => ResearchOnly
                ? Research(kind, targetDate, cancellationToken)
                : publishingService.Publish(kind, targetDate, cancellationToken)));
        }
        finally
        {
            if (!ResearchOnly)
            {
                await sitePublishService.Publish(cancellationToken);
            }
        }
    }

    private async Task Research(WatchlistKind kind, DateOnly targetDate, CancellationToken cancellationToken)
    {
        var days = kind.Days(targetDate);
        var events = await researchService.LoadEvents(days, cancellationToken);
        if (events.Count == 0)
        {
            return;
        }

        var (watchlist, cost) = await researchService.Research(kind, days, events, cancellationToken);
        var posts = SportsGuideFormatter.CreatePosts(watchlist, kind, targetDate);
        logger.LogInformation(
            "{kind} watchlist (~${cost:N2}):\n{json}\n\n{post1}\n\n{post2}\n\n{post3}",
            kind,
            cost,
            JsonSerializer.Serialize(watchlist, JsonOptions),
            posts.Post1,
            posts.Post2,
            posts.Post3
        );
    }
}