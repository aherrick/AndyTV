using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace AndyTV.Watchlist.Services;

// The public container hosts the rendered site.zip and Instagram cards; editions stay private.
public sealed class BlobStore(AppSettings settings)
{
    private BlobContainerClient Container => new(settings.BlobConnectionString, "andytv-watchlist");

    // Never public: holds state such as the refreshed Instagram token.
    private BlobContainerClient PrivateContainer => new(settings.BlobConnectionString, "andytv-watchlist-private");

    public async Task<string> ReadPrivate(string blobName, CancellationToken cancellationToken = default)
    {
        var blob = PrivateContainer.GetBlobClient(blobName);
        return await blob.ExistsAsync(cancellationToken)
            ? (await blob.DownloadContentAsync(cancellationToken)).Value.Content.ToString()
            : null;
    }

    public Task WritePrivate(string blobName, string value, CancellationToken cancellationToken = default) =>
        Upload(false, blobName, BinaryData.FromString(value), cancellationToken: cancellationToken);

    // Names sort by UTC start time, so newest-first is a reverse name sort.
    public Task SaveRun(WatchlistRun run) =>
        WritePrivate($"runs/{RunName(run)}.json", JsonSerializer.Serialize(run, JsonSerializerOptions.Web));

    // Kept outside runs/ because ReadRuns parses every blob there as JSON.
    public Task SaveRunLog(WatchlistRun run, string text) =>
        Upload(false, LogName(run), BinaryData.FromString(text), "text/plain; charset=utf-8");

    // Short-lived read link so the log stays private.
    public Uri RunLogUri(WatchlistRun run) =>
        PrivateContainer.GetBlobClient(LogName(run)).GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));

    private static string RunName(WatchlistRun run) => $"{run.Started:yyyyMMdd-HHmmss}-{run.Kind}";

    private static string LogName(WatchlistRun run) => $"logs/{RunName(run)}.txt";

    public async Task<List<WatchlistRun>> ReadRuns(int count) =>
        await ReadJson<WatchlistRun>((await ListPrivate("runs/")).OrderDescending().Take(count));

    // Uploads a card PNG to a public container and returns its blob URL.
    public Task<Uri> UploadImage(string blobName, byte[] png, CancellationToken cancellationToken = default) =>
        Upload(true, blobName, BinaryData.FromBytes(png), "image/png", cancellationToken: cancellationToken);

    // The deploy workflow downloads this zip of every rendered page and publishes it as the site.
    public Task<Uri> PublishSite(byte[] zip, CancellationToken cancellationToken = default) =>
        Upload(true, "site.zip", BinaryData.FromBytes(zip), "application/zip", "no-cache", cancellationToken);

    // Keyed by date, so a re-run replaces that day's edition and every past day stays archived.
    public Task SaveEdition(SiteEdition edition, WatchlistKind kind, CancellationToken cancellationToken = default) =>
        WritePrivate(kind.EditionBlobName(edition.TargetDate), JsonSerializer.Serialize(edition, JsonSerializerOptions.Web), cancellationToken);

    // Every saved edition of a kind, oldest first.
    public async Task<List<SiteEdition>> ReadEditions(WatchlistKind kind, CancellationToken cancellationToken = default) =>
        await ReadJson<SiteEdition>((await ListPrivate(kind.EditionPrefix(), cancellationToken)).Order(), cancellationToken);

    private async Task<List<string>> ListPrivate(string prefix, CancellationToken cancellationToken = default)
    {
        var container = PrivateContainer;
        List<string> names = [];
        if (!await container.ExistsAsync(cancellationToken))
        {
            return names;
        }

        await foreach (var blob in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, cancellationToken))
        {
            names.Add(blob.Name);
        }
        return names;
    }

    private async Task<List<T>> ReadJson<T>(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var container = PrivateContainer;
        return
        [
            .. await Task.WhenAll(
                names.Select(async name =>
                    (await container.GetBlobClient(name).DownloadContentAsync(cancellationToken)).Value.Content.ToObjectFromJson<T>(JsonSerializerOptions.Web)
                )
            ),
        ];
    }

    private async Task<Uri> Upload(
        bool isPublic,
        string blobName,
        BinaryData content,
        string contentType = null,
        string cacheControl = null,
        CancellationToken cancellationToken = default
    )
    {
        var container = isPublic ? Container : PrivateContainer;
        await container.CreateIfNotExistsAsync(
            isPublic ? PublicAccessType.Blob : PublicAccessType.None,
            cancellationToken: cancellationToken
        );

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new() { ContentType = contentType, CacheControl = cacheControl } },
            cancellationToken
        );
        return blob.Uri;
    }
}
