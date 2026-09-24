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
    private const string PreviewDirectory = "publish";

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
        var watchlist = await gmailService.GetLatest(targetDate, kind, cancellationToken);
        if (watchlist is null || watchlist.BestWatches.Count == 0)
        {
            logger.LogInformation("No emailed {kind} watchlist with games found for {targetDate}.", kind, targetDate);
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
        // Local preview runs must only remove their local copy, never the live blob.
        if (settings.PublishLocal)
        {
            var path = Path.Combine(PreviewDirectory, WatchlistKind.Weekend.FeedFileName());
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            logger.LogInformation("Cleared local weekend feed.");
        }
        else if (settings.CanPublishSite)
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
        // Write the same JSON to either disk or the site's public blob. Save it
        // before social publishing so it is available even if a later step fails.
        if (settings.PublishLocal)
        {
            Directory.CreateDirectory(PreviewDirectory);
            var path = Path.GetFullPath(Path.Combine(PreviewDirectory, kind.FeedFileName()));
            await File.WriteAllTextAsync(path, json, cancellationToken);
            logger.LogInformation("Saved {kind} feed preview to {path}.", kind, path);
        }
        else if (settings.CanPublishSite)
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
        // Both local previews and Instagram use the same rendered cards and
        // Cloudflare capture call. Only the PNG destination changes in local mode.
        if (!settings.CanScreenshot)
        {
            return;
        }

        var instaDir = Path.GetFullPath(Path.Combine(PreviewDirectory, "insta"));
        if (settings.PublishLocal)
        {
            Directory.CreateDirectory(instaDir);
        }

        var imageUrls = new List<Uri>();
        foreach (var card in InstaCardRenderer.Render(watchlist, targetDate))
        {
            var png = await screenshotService.Capture(card.Html, cancellationToken);
            var fileName = Path.ChangeExtension(card.Name, ".png");
            if (settings.PublishLocal)
            {
                await File.WriteAllBytesAsync(Path.Combine(instaDir, fileName), png, cancellationToken);
            }
            else
            {
                var blobName = $"{targetDate:yyyyMMdd}/{fileName}";
                imageUrls.Add(await blobStore.UploadImage(blobName, png, cancellationToken));
            }
        }

        // Instagram requires public URLs; local files are only previews.
        if (settings.PublishLocal)
        {
            logger.LogInformation("Saved daily Instagram previews to {instaDir}.", instaDir);
        }
        else if (settings.CanPublishInstagram)
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
        // enabled only when all four X credentials are present, including in local mode.
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
