using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

/// <summary>
/// Runs one Daily or Weekend edition top to bottom and saves a WatchlistRun summary,
/// even on failure. Timer selection belongs to AndyTVWatchlistFn.
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
            // 1. Sunday's daily run expires the weekend feed.
            if (kind == WatchlistKind.Daily && targetDate.DayOfWeek == DayOfWeek.Sunday && settings.CanPublishSite)
            {
                await blobStore.DeleteWeekendData(cancellationToken);
            }

            // 2. Load schedule events; none leaves the existing feed alone.
            var days = kind.Days(targetDate);
            var events = await researchService.LoadEvents(days, cancellationToken);
            run.Events = events.Count;
            if (events.Count == 0)
            {
                return;
            }

            // 3. Copilot ranks and enriches the events.
            (var watchlist, run.Cost) = await researchService.Research(kind, days, events, cancellationToken);

            // 4. Publish the site feed before social so it's live even if a later step fails.
            if (settings.CanPublishSite)
            {
                var json = JsonSerializer.Serialize(WatchlistSiteBuilder.Build(watchlist, kind, targetDate), JsonSerializerOptions.Web);
                await blobStore.PublishData(json, kind, cancellationToken);
                run.Feed = true;
            }

            // 5. Social posts.
            run.InstagramUrl = await PublishInstagram(watchlist, kind, targetDate, cancellationToken);
            run.XPostId = await PublishXThread(watchlist, kind, targetDate, cancellationToken);
        }
        catch (Exception ex)
        {
            run.Error = ex.Message;
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

        return await instagramService.PublishCarousel(imageUrls, cancellationToken);
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
