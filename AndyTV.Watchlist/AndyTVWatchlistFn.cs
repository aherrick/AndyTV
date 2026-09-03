using System;
using System.Threading;
using System.Threading.Tasks;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist;

public class AndyTVWatchlistFn(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<AndyTVWatchlistFn>();

    [Function(nameof(AndyTVWatchlistFn))]
    public async Task Run(
        //#if DEBUG

        [TimerTrigger("0 0 * * * *", RunOnStartup = true)] // jsut for testing for now
        // [TimerTrigger("0 0 8 * * *", RunOnStartup = true)]
        //   TimerInfo myTimer,
        //#else
        //        [TimerTrigger("0 0 8 * * *")] TimerInfo myTimer,
        //#endif
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation("Sports guide run started at: {executionTime}", DateTime.Now);

        var settings = AppSettings.Load();
        var easternTimeZone = EasternTimeZone.Get();
        var easternNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, easternTimeZone);
        var targetDate = DateOnly.FromDateTime(easternNow.DateTime);

        using var sportsHttpClient = new HttpClient();
        var sportsService = new ApiSportsService(
            sportsHttpClient,
            settings.SportsApiKey,
            easternTimeZone
        );

        var events = await sportsService.GetEventsForDateAsync(targetDate, cancellationToken);
        _logger.LogInformation(
            "Verified events for {targetDate}: {count}",
            targetDate,
            events.Count
        );

        if (events.Count == 0)
        {
            _logger.LogInformation("No eligible events were returned by the sports APIs.");
            return;
        }

        var guideService = new SportsGuideService(settings);
        var guide = await guideService.CreateGuideAsync(events, easternNow, cancellationToken);

        var formatter = new SportsGuideFormatter();
        var posts = formatter.CreatePosts(events, guide, targetDate);

        _logger.LogInformation(
            "{post1}\n\n{post2}\n\n{post3}",
            posts.Post1,
            posts.Post2,
            posts.Post3
        );

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

        _logger.LogInformation("Thread posted: https://x.com/i/web/status/{postId}", postId);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation(
                "Next timer schedule at: {nextSchedule}",
                myTimer.ScheduleStatus.Next
            );
        }
    }
}