using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

/// <summary>
/// Processes one watchlist for an Eastern calendar date: expire the weekend feed
/// when needed, load the matching email, enrich games, and publish the site data.
/// Daily editions also produce Instagram cards and X posts; weekend editions are
/// data-only. Timer selection belongs to AndyTVWatchlistFn, not this service.
/// </summary>
public sealed class WatchlistPublishingService(
    GmailWatchlistService gmailService,
    CloudflareScreenshotService screenshotService,
    BlobStore blobStore,
    InstagramPublishService instagramService,
    AppSettings settings,
    ApiSportsScoreService scoreService,
    ILogger<WatchlistPublishingService> logger
)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task PublishAsync(
        WatchlistKind kind,
        DateOnly targetDate,
        CancellationToken cancellationToken = default
    )
    {
        // Expiration does not depend on a new daily email. Do this before Gmail,
        // score enrichment, or social publishing can return early or fail.
        if (kind == WatchlistKind.Daily && targetDate.DayOfWeek == DayOfWeek.Sunday)
        {
            await DeleteWeekendFeedAsync(cancellationToken);
        }

        // A missing or empty email leaves the existing feed alone.
        var watchlist = await gmailService.GetLatest(kind, cancellationToken);
        if (watchlist is null || watchlist.BestWatches.Count == 0)
        {
            logger.LogInformation("No emailed {kind} watchlist with games found.", kind);
            return;
        }

        await scoreService.EnrichAsync(watchlist.BestWatches, cancellationToken);

        // Both editions use the same email schema and render-ready site model.
        // The kind controls the subject and filename, not the shape of the JSON.
        var json = JsonSerializer.Serialize(WatchlistSiteBuilder.Build(watchlist, targetDate), JsonOptions);
        await PublishFeedAsync(kind, json, cancellationToken);

        // Only the daily guide has social output. Friday's weekend processing
        // stops after publishing its own feed, leaving daily posts and cards alone.
        if (kind == WatchlistKind.Weekend)
        {
            return;
        }

        await PublishInstagramAsync(watchlist, targetDate, cancellationToken);
        await PublishXThreadAsync(watchlist, targetDate, cancellationToken);
    }

    private async Task DeleteWeekendFeedAsync(CancellationToken cancellationToken)
    {
        // Deletion is idempotent: an absent file is already the desired state.
        if (settings.CanPublishSite)
        {
            await blobStore.DeleteWeekendData(cancellationToken);
            logger.LogInformation("Cleared published weekend feed.");
        }
    }

    private async Task PublishFeedAsync(
        WatchlistKind kind,
        string json,
        CancellationToken cancellationToken
    )
    {
        // Save before social publishing so the feed is live even if a later step fails.
        if (settings.CanPublishSite)
        {
            var url = await blobStore.PublishData(json, kind, cancellationToken);
            logger.LogInformation("Published {kind} feed to {url}.", kind, url);
        }
    }

    private async Task PublishInstagramAsync(
        DailyWatchlist watchlist,
        DateOnly targetDate,
        CancellationToken cancellationToken
    )
    {
        if (!settings.CanScreenshot)
        {
            return;
        }

        var imageUrls = new List<Uri>();
        foreach (var card in InstaCardRenderer.Render(watchlist, targetDate))
        {
            var png = await screenshotService.Capture(card.Html, cancellationToken);
            var blobName = $"{targetDate:yyyyMMdd}/{Path.ChangeExtension(card.Name, ".png")}";
            imageUrls.Add(await blobStore.UploadImage(blobName, png, cancellationToken));
        }

        if (settings.CanPublishInstagram)
        {
            var caption = $"AndyTV Watchlist — Best Sports Today\n{targetDate:dddd, MMMM d}";
            await instagramService.PublishCarousel(imageUrls, caption, cancellationToken);
        }
    }

    private async Task PublishXThreadAsync(
        DailyWatchlist watchlist,
        DateOnly targetDate,
        CancellationToken cancellationToken
    )
    {
        // Always log the three formatted posts for inspection. Actual posting is
        // enabled only when all four X credentials are present.
        var posts = SportsGuideFormatter.CreatePosts(watchlist, targetDate);
        logger.LogInformation("{post1}\n\n{post2}\n\n{post3}", posts.Post1, posts.Post2, posts.Post3);
        if (!settings.CanPostToX)
        {
            logger.LogInformation("X preview only. Add the four X_ secrets to publish the thread.");
            return;
        }

        using var xPostingService = new XPostingService(
            settings.XConsumerKey!,
            settings.XConsumerSecret!,
            settings.XAccessToken!,
            settings.XAccessTokenSecret!
        );
        var postId = await xPostingService.PostThreadAsync(posts, cancellationToken);
        logger.LogInformation("Thread posted: https://x.com/i/web/status/{postId}", postId);
    }
}
