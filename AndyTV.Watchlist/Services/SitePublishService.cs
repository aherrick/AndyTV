using System.IO.Compression;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Octokit;

namespace AndyTV.Watchlist.Services;

// Renders every page from the saved editions into site.zip, uploads it, then has GitHub Actions deploy it.
// A deploy replaces the whole site, so each zip carries the full archive, not just today.
public sealed class SitePublishService(BlobStore blobStore, AppSettings settings)
{
    public async Task Publish(CancellationToken cancellationToken = default)
    {
        if (!settings.CanPublishSite)
        {
            return;
        }

        var dailies = await blobStore.ReadEditions(WatchlistKind.Daily, cancellationToken);
        if (dailies.Count == 0)
        {
            return;
        }

        var weekends = (await blobStore.ReadEditions(WatchlistKind.Weekend, cancellationToken)).ToDictionary(edition => edition.TargetDate);
        // Friday's weekend edition stays up through Saturday.
        SiteEdition Weekend(DateOnly day) => weekends.GetValueOrDefault(day) ?? weekends.GetValueOrDefault(day.AddDays(-1));

        using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Add(string path, string content)
            {
                using var writer = new StreamWriter(archive.CreateEntry(path).Open());
                writer.Write(content);
            }

            // Today, not the latest daily's date, so a failed daily run never brings back an expired weekend.
            Add("index.html", SitePageRenderer.Render(dailies[^1], Weekend(EasternTimeZone.Today)));
            for (var i = 0; i < dailies.Count; i++)
            {
                var date = dailies[i].TargetDate;
                var nav = new ArchiveNav(dailies.ElementAtOrDefault(i - 1)?.TargetDate, dailies.ElementAtOrDefault(i + 1)?.TargetDate);
                Add($"{SitePageRenderer.ArchivePath(date).TrimStart('/')}index.html", SitePageRenderer.Render(dailies[i], Weekend(date), nav));
            }
            Add("archive/index.html", SitePageRenderer.RenderArchive(dailies));
            Add("sitemap.xml", SitePageRenderer.RenderSitemap(dailies));
        }
        await blobStore.PublishSite(zip.ToArray(), cancellationToken);

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
}
