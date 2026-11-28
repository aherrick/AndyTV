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
        // Friday's weekend edition stays up through Saturday, when Saturday's daily freshens its odds.
        SiteEdition Weekend(DateOnly day, SiteEdition daily) =>
            (weekends.GetValueOrDefault(day) ?? weekends.GetValueOrDefault(day.AddDays(-1))) is { } weekend
                ? WithOddsFrom(weekend, daily)
                : null;

        await using var zip = new MemoryStream();
        await using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Add(string path, string content)
            {
                using var writer = new StreamWriter(archive.CreateEntry(path).Open());
                writer.Write(content);
            }

            // Today, not the latest daily's date, so a failed daily run never brings back an expired weekend.
            Add("index.html", SitePageRenderer.Render(dailies[^1], Weekend(EasternTimeZone.Today, dailies[^1])));
            for (var i = 0; i < dailies.Count; i++)
            {
                var date = dailies[i].TargetDate;
                var nav = new ArchiveNav(dailies.ElementAtOrDefault(i - 1)?.TargetDate, dailies.ElementAtOrDefault(i + 1)?.TargetDate);
                Add($"{SitePageRenderer.ArchivePath(date).TrimStart('/')}index.html", SitePageRenderer.Render(dailies[i], Weekend(date, dailies[i]), nav));
            }
            Add("archive/index.html", SitePageRenderer.RenderArchive(dailies));
            foreach (var year in dailies.GroupBy(edition => edition.TargetDate.Year))
            {
                Add($"{year.Key}/index.html", SitePageRenderer.RenderArchive(dailies, year.Key));
                foreach (var month in year.Select(edition => edition.TargetDate.Month).Distinct())
                {
                    Add($"{year.Key}/{month:00}/index.html", SitePageRenderer.RenderArchive(dailies, year.Key, month));
                }
            }
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

    // By Saturday, Friday's weekend odds are a day old, so its games that Saturday's daily also has take that feed's
    // lines (Sunday's games keep Friday's). Only the page changes; the saved edition keeps Friday's odds.
    private static SiteEdition WithOddsFrom(SiteEdition weekend, SiteEdition daily) =>
        weekend with
        {
            Watchlist = weekend.Watchlist with
            {
                BestWatches =
                [
                    .. weekend.Watchlist.BestWatches.Select(game =>
                        daily.Events.FindGame(game.Matchup, game.StartTimeIso) is { } fresh ? game.WithOddsFrom(fresh) : game
                    ),
                ],
            },
            Events =
            [
                .. weekend.Events.Select(e =>
                    daily.Events.FindGame(e.Matchup, e.StartTimeIso) is { } fresh ? e.WithOddsFrom(fresh) : e
                ),
            ],
        };
}
