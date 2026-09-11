using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist;

public class AndyTVWatchlistFn(
    ILoggerFactory loggerFactory,
    GmailWatchlistService gmailService,
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

        [TimerTrigger("0 30 7,8 * * *", RunOnStartup = true)] TimerInfo myTimer,
#else
        // Host stays on UTC; fires 07:30 + 08:30 UTC and the Eastern-hour guard below runs work only at 3:30 AM ET (DST-proof).
        [TimerTrigger("0 30 7,8 * * *")] TimerInfo myTimer,
#endif
        CancellationToken cancellationToken
    )
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Watchlist run started at: {executionTime}", DateTime.Now);
        }

        var easternNow = EasternTimeZone.Now;

#if !DEBUG
        // Only one of the two UTC firings lands on 3 AM Eastern; skip the other so the work runs once at 3:30 AM ET.
        if (easternNow.Hour != 3)
        {
            _logger.LogInformation("Skipping {easternNow:h:mm tt} ET run; waiting for 3:30 AM ET.", easternNow);
            return;
        }
#endif

        var targetDate = DateOnly.FromDateTime(easternNow.DateTime);

        var watchlist = await gmailService.GetLatest(targetDate, cancellationToken);

        if (watchlist is null || watchlist.BestWatches.Count == 0)
        {
            _logger.LogInformation("No emailed watchlist found for {targetDate}.", targetDate);
            return;
        }

        var html = WatchlistSiteBuilder.BuildHtml(watchlist, targetDate);

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
            var cards = InstaCardRenderer.Render(watchlist, targetDate);
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

        var posts = SportsGuideFormatter.CreatePosts(watchlist, targetDate);

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

        // Local preview: write the finished page and Instagram card images to disk as the final step.
        if (settings.PublishLocal)
        {
            Directory.CreateDirectory("publish");
            var path = Path.GetFullPath(Path.Combine("publish", "index.html"));
            await File.WriteAllTextAsync(path, html, cancellationToken);

            var instaDir = Path.GetFullPath(Path.Combine("publish", "insta"));
            if (settings.CanScreenshot)
            {
                Directory.CreateDirectory(instaDir);
                foreach (var card in InstaCardRenderer.Render(watchlist, targetDate))
                {
                    var png = await screenshotService.Capture(card.Html, cancellationToken);
                    var pngPath = Path.Combine(instaDir, Path.ChangeExtension(card.Name, ".png"));
                    await File.WriteAllBytesAsync(pngPath, png, cancellationToken);
                }
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Saved local preview to {path} (cards in {instaDir})", path, instaDir);
            }
        }
    }
}
