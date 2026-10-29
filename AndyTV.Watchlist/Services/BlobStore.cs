using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace AndyTV.Watchlist.Services;

// The public container hosts the rendered index.html and Instagram cards; editions stay private.
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

    public async Task<List<WatchlistRun>> ReadRuns(int count)
    {
        var container = PrivateContainer;
        if (!await container.ExistsAsync())
        {
            return [];
        }

        List<string> names = [];
        await foreach (var blob in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, "runs/", default))
        {
            names.Add(blob.Name);
        }

        var runs = await Task.WhenAll(
            names.OrderDescending().Take(count).Select(async name =>
                (await container.GetBlobClient(name).DownloadContentAsync()).Value.Content.ToObjectFromJson<WatchlistRun>(JsonSerializerOptions.Web)
            )
        );
        return [.. runs];
    }

    // Uploads a card PNG to a public container and returns its blob URL.
    public Task<Uri> UploadImage(string blobName, byte[] png, CancellationToken cancellationToken = default) =>
        Upload(true, blobName, BinaryData.FromBytes(png), "image/png", cancellationToken: cancellationToken);

    // The deploy workflow downloads this and publishes it as the site's index.html.
    public Task<Uri> PublishPage(string html, CancellationToken cancellationToken = default) =>
        Upload(true, "index.html", BinaryData.FromString(html), "text/html; charset=utf-8", "no-cache", cancellationToken);

    // Each kind's latest edition, kept so later days can re-render the page (e.g. Saturday keeps Friday's weekend).
    public Task SaveEdition(SiteEdition edition, WatchlistKind kind, CancellationToken cancellationToken = default) =>
        WritePrivate(kind.EditionBlobName(), JsonSerializer.Serialize(edition, JsonSerializerOptions.Web), cancellationToken);

    // Null when that kind isn't published (no weekend outside Friday-Saturday).
    public async Task<SiteEdition> ReadEdition(WatchlistKind kind, CancellationToken cancellationToken = default) =>
        await ReadPrivate(kind.EditionBlobName(), cancellationToken) is { } json
            ? JsonSerializer.Deserialize<SiteEdition>(json, JsonSerializerOptions.Web)
            : null;

    // Sunday expires the weekend edition; an already-missing blob is a no-op.
    public async Task DeleteWeekendEdition(CancellationToken cancellationToken = default) =>
        await PrivateContainer.GetBlobClient(WatchlistKind.Weekend.EditionBlobName()).DeleteIfExistsAsync(cancellationToken: cancellationToken);

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
