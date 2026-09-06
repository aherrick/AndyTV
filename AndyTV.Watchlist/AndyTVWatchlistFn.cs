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
    InstaCardRenderer cardRenderer,
    CloudflareScreenshotService screenshotService,
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

        var feedResults = await Task.WhenAll(
            feeds.Select(feed => feed.GetEventsForDateAsync(targetDate, cancellationToken))
        );

        var events = feedResults
            .SelectMany(feedEvents => feedEvents)
            .OrderBy(sportsEvent => sportsEvent.StartTimeEastern)
            .ToList();

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

        var cards = cardRenderer.Render(events, guide, targetDate);
        var dayDir = CardStorage.DayDir(targetDate);
        Directory.CreateDirectory(dayDir);

        foreach (var card in cards)
        {
            if (settings.CanScreenshot)
            {
                var png = await screenshotService.Capture(card.Html, cancellationToken);
                var pngName = Path.ChangeExtension(card.Name, ".png");
                await File.WriteAllBytesAsync(
                    Path.Combine(dayDir, pngName),
                    png,
                    cancellationToken
                );
            }
            else
            {
                // No Cloudflare secrets yet: keep the raw HTML so the cards are still previewable.
                await File.WriteAllTextAsync(
                    Path.Combine(dayDir, card.Name),
                    card.Html,
                    cancellationToken
                );
            }
        }

        _logger.LogInformation(
            "Saved {count} cards to {dir} (screenshots: {mode}). View: /api/cards/{day}",
            cards.Count,
            dayDir,
            settings.CanScreenshot ? "on" : "html-only",
            CardStorage.DayFolderName(targetDate)
        );

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