using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

/// <summary>
/// Processes one watchlist for an Eastern calendar date: expire the weekend feed
/// when needed, research the watchlist, and publish the site data.
/// Daily and weekend editions also produce Instagram cards and X posts.
/// Timer selection belongs to AndyTVWatchlistFn, not this service.
/// </summary>
public sealed class WatchlistPublishingService(
    WatchlistResearchService researchService,
    CloudflareScreenshotService screenshotService,
    BlobStore blobStore,
    InstagramPublishService instagramService,
    AppSettings settings,
    ILogger<WatchlistPublishingService> logger
)
{
    public async Task Publish(
        WatchlistKind kind,
        DateOnly targetDate,
        CancellationToken cancellationToken = default
    )
    {
        var run = new WatchlistRun { Started = DateTimeOffset.UtcNow, Kind = kind.ToString() };
        try
        {
            await Run(kind, targetDate, run, cancellationToken);
        }
        catch (Exception ex)
        {
            run.Error = ex.Message;
            logger.LogError(ex, "{kind} run failed.", kind);
            throw;
        }
        finally
        {
            run.Duration = (DateTimeOffset.UtcNow - run.Started).ToString(@"mm\:ss");
            if (settings.CanPublishSite)
            {
                await blobStore.SaveRun(run);
            }
        }
    }

    private async Task Run(WatchlistKind kind, DateOnly targetDate, WatchlistRun run, CancellationToken cancellationToken)
    {
        // Expiration does not depend on a new daily watchlist; an absent file is already the desired state.
        if (kind == WatchlistKind.Daily && targetDate.DayOfWeek == DayOfWeek.Sunday && settings.CanPublishSite)
        {
            await blobStore.DeleteWeekendData(cancellationToken);
            logger.LogInformation("Cleared published weekend feed.");
        }

        // No events leaves the existing feed alone.
        var watchlist = await researchService.Create(kind, targetDate, run, cancellationToken);
        if (watchlist is null)
        {
            logger.LogInformation("No {kind} events to rank.", kind);
            return;
        }

        // Save before social publishing so the feed is live even if a later step fails.
        if (settings.CanPublishSite)
        {
            var json = JsonSerializer.Serialize(WatchlistSiteBuilder.Build(watchlist, kind, targetDate), JsonSerializerOptions.Web);
            var url = await blobStore.PublishData(json, kind, cancellationToken);
            logger.LogInformation("Published {kind} feed to {url}.", kind, url);
            run.Feed = true;
        }

        run.InstagramId = await PublishInstagram(watchlist, kind, targetDate, cancellationToken);
        run.XPostId = await PublishXThread(watchlist, kind, targetDate, cancellationToken);
    }

    private async Task<string?> PublishInstagram(
        DailyWatchlist watchlist,
        WatchlistKind kind,
        DateOnly targetDate,
        CancellationToken cancellationToken
    )
    {
        if (!settings.CanScreenshot)
        {
            return null;
        }

        var imageUrls = new List<Uri>();
        foreach (var card in InstaCardRenderer.Render(watchlist, kind, targetDate))
        {
            var png = await screenshotService.Capture(card.Html, cancellationToken);
            // Friday's Daily and Weekend cards share a date folder.
            var blobName = $"{targetDate:yyyyMMdd}/{kind.ToString().ToLowerInvariant()}/{Path.ChangeExtension(card.Name, ".png")}";
            imageUrls.Add(await blobStore.UploadImage(blobName, png, cancellationToken));
        }

        if (!settings.CanPublishInstagram)
        {
            return null;
        }

        var caption = $"AndyTV Watchlist — Best Sports {SportsFormat.Period(kind)}\n{SportsFormat.Dates(kind, targetDate)}";
        return await instagramService.PublishCarousel(imageUrls, caption, cancellationToken);
    }

    private async Task<string?> PublishXThread(
        DailyWatchlist watchlist,
        WatchlistKind kind,
        DateOnly targetDate,
        CancellationToken cancellationToken
    )
    {
        // Always log the three formatted posts for inspection. Actual posting is
        // enabled only when all four X credentials are present.
        var posts = SportsGuideFormatter.CreatePosts(watchlist, kind, targetDate);
        logger.LogInformation("{post1}\n\n{post2}\n\n{post3}", posts.Post1, posts.Post2, posts.Post3);
        if (!settings.CanPostToX)
        {
            logger.LogInformation("X preview only. Add the four X_ secrets to publish the thread.");
            return null;
        }

        using var xPostingService = new XPostingService(settings);
        return await xPostingService.PostThread(posts, cancellationToken);
    }
}
