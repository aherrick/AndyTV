using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist;

public class AndyTVWatchlistFn(
    ILoggerFactory loggerFactory,
    IEnumerable<SportsFeedService> feeds,
    SportsGuideService guideService,
    AppSettings settings
)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<AndyTVWatchlistFn>();

    [Function(nameof(AndyTVWatchlistFn))]
    public async Task Run(
#if DEBUG

        [TimerTrigger("0 0 8 * * *", RunOnStartup = true)] TimerInfo myTimer, // runs at 3/4 am EST
#else
        [TimerTrigger("0 0 8 * * *")] TimerInfo myTimer,
#endif
        CancellationToken cancellationToken
    )
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Sports guide run started at: {executionTime}", DateTime.Now);
        }

        var easternNow = EasternTimeZone.Now;
        var targetDate = DateOnly.FromDateTime(easternNow.DateTime);

        var collected = new List<SportsEvent>();

        var activeFeeds = feeds;
#if DEBUG
        // Debug a single feed in isolation.
        activeFeeds = feeds.OfType<EspnRacingService>();
#endif

        foreach (var feed in activeFeeds)
        {
            collected.AddRange(await feed.GetEventsForDateAsync(targetDate, cancellationToken));
        }

        var events = collected.OrderBy(sportsEvent => sportsEvent.StartTimeEastern).ToList();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Verified events for {targetDate}: {count}",
                targetDate,
                events.Count
            );
        }

        if (events.Count == 0)
        {
            _logger.LogInformation("No eligible events were returned by the sports APIs.");
            return;
        }

        var guide = await guideService.CreateGuideAsync(events, easternNow, cancellationToken);

        var posts = SportsGuideFormatter.CreatePosts(events, guide, targetDate);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "{post1}\n\n{post2}\n\n{post3}",
                posts.Post1,
                posts.Post2,
                posts.Post3
            );
        }

        if (!settings.CanPostToX)
        {
            _logger.LogInformation("Preview only. Add the four X_ secrets to publish the thread.");
            return;
        }

        using var xPostingService = new XPostingService(
            settings.XConsumerKey!,
            settings.XConsumerSecret!,
            settings.XAccessToken!,
            settings.XAccessTokenSecret!
        );
        var postId = await xPostingService.PostThreadAsync(posts, cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Thread posted: https://x.com/i/web/status/{postId}", postId);
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation(
                "Next timer schedule at: {nextSchedule}",
                myTimer.ScheduleStatus.Next
            );
        }
    }
}