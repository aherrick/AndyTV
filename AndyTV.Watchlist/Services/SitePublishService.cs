using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Octokit;

namespace AndyTV.Watchlist.Services;

// Renders index.html from the published feeds, uploads it, then has GitHub Actions deploy it to the site.
public sealed class SitePublishService(BlobStore blobStore, AppSettings settings)
{
    public async Task Publish(CancellationToken cancellationToken = default)
    {
        if (!settings.CanPublishSite || await Read(WatchlistKind.Daily, cancellationToken) is not { } daily)
        {
            return;
        }

        var html = SitePageRenderer.Render(daily, await Read(WatchlistKind.Weekend, cancellationToken));
        await blobStore.PublishPage(html, cancellationToken);

        if (!settings.CanDeploySite)
        {
            return;
        }

        var github = new GitHubClient(new ProductHeaderValue("AndyTV.Watchlist"))
        {
            Credentials = new Credentials(settings.GitHubDeployToken),
        };
        await github.Actions.Workflows.CreateDispatch("aherrick", "AndyTV", "publish-watchlist-site.yml", new CreateWorkflowDispatch("main"));
    }

    // Null when that edition isn't published (e.g. no weekend outside Friday-Saturday).
    private async Task<SiteEdition> Read(WatchlistKind kind, CancellationToken cancellationToken) =>
        await blobStore.ReadFeed<WatchlistSiteModel>(kind.FeedFileName(), cancellationToken) is { } model
            ? new(model, await blobStore.ReadFeed<List<ScheduleEvent>>(kind.ScheduleFileName(), cancellationToken) ?? [])
            : null;
}
