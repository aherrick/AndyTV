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
    CloudflareScreenshotService screenshotService,
    BlobStore blobStore,
    InstagramPublishService instagramService,
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

        var html = WatchlistSiteBuilder.BuildHtml(events, guide, targetDate);

        if (!settings.PublishLocal && settings.CanPublishSite)
        {
            var url = await blobStore.PublishSite(html, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Published site to {url}", url);
            }
        }

        // Instagram carousel uses blob-hosted card images; skipped for local previews.
        if (!settings.PublishLocal && settings.CanScreenshot)
        {
            var day = targetDate.ToString("yyyyMMdd");
            var cards = InstaCardRenderer.Render(events, guide, targetDate);
            var imageUrls = new List<Uri>();
            foreach (var card in cards)
            {
                var png = await screenshotService.Capture(card.Html, cancellationToken);
                var blobName = $"{day}/{Path.ChangeExtension(card.Name, ".png")}";
                imageUrls.Add(await blobStore.UploadImage(blobName, png, cancellationToken));
            }

            if (settings.CanPublishInstagram)
            {
                var caption = $"AndyTV Watchlist — Best Sports Today\n{targetDate:dddd, MMMM d}";
                await instagramService.PublishCarousel(imageUrls, caption, cancellationToken);
            }
        }

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

        if (settings.CanPostToX)
        {
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
        else
        {
            _logger.LogInformation("Preview only. Add the four X_ secrets to publish the thread.");
        }

        // Local preview: write the finished page and Instagram cards to disk as the final step.
        if (settings.PublishLocal)
        {
            Directory.CreateDirectory("publish");
            var path = Path.GetFullPath(Path.Combine("publish", "index.html"));
            await File.WriteAllTextAsync(path, html, cancellationToken);

            var instaDir = Path.GetFullPath(Path.Combine("publish", "insta"));
            Directory.CreateDirectory(instaDir);
            foreach (var card in InstaCardRenderer.Render(events, guide, targetDate))
            {
                await File.WriteAllTextAsync(
                    Path.Combine(instaDir, card.Name),
                    card.Html,
                    cancellationToken
                );
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Saved local preview to {path} (cards in {instaDir})", path, instaDir);
            }
        }
    }
}