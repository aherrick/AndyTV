using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace AndyTV.Watchlist.Services;

// One public container hosts latest.json, latest_weekend.json, and Instagram cards.
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

    public async Task WritePrivate(string blobName, string value, CancellationToken cancellationToken = default)
    {
        var container = PrivateContainer;
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        await container.GetBlobClient(blobName).UploadAsync(BinaryData.FromString(value), overwrite: true, cancellationToken);
    }

    // Names sort by UTC start time, so newest-first is a reverse name sort.
    public Task SaveRun(WatchlistRun run) =>
        WritePrivate($"runs/{RunName(run)}.json", JsonSerializer.Serialize(run, JsonSerializerOptions.Web));

    // Kept outside runs/ because ReadRuns parses every blob there as JSON.
    public async Task SaveRunLog(WatchlistRun run, string text)
    {
        var container = PrivateContainer;
        await container.CreateIfNotExistsAsync(PublicAccessType.None);
        await container.GetBlobClient($"logs/{RunName(run)}.txt").UploadAsync(
            BinaryData.FromString(text),
            new BlobUploadOptions { HttpHeaders = new() { ContentType = "text/plain; charset=utf-8" } }
        );
    }

    // Short-lived read link so the log stays private.
    public Uri RunLogUri(WatchlistRun run) =>
        PrivateContainer.GetBlobClient($"logs/{RunName(run)}.txt")
            .GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));

    private static string RunName(WatchlistRun run) => $"{run.Started:yyyyMMdd-HHmmss}-{run.Kind}";

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
        Upload(blobName, BinaryData.FromBytes(png), "image/png", null, cancellationToken);

    // Uploads a watchlist feed to the container root and returns its blob URL.
    public Task<Uri> PublishData(string json, WatchlistKind kind, CancellationToken cancellationToken = default) =>
        Upload(kind.FeedFileName(), BinaryData.FromString(json), "application/json; charset=utf-8", "no-cache", cancellationToken);

    // Sunday expires only the weekend feed. No container creation or deletion is
    // needed here, and an already-missing blob is a successful no-op.
    public Task DeleteWeekendData(CancellationToken cancellationToken = default) =>
        Container.GetBlobClient(WatchlistKind.Weekend.FeedFileName())
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);

    private async Task<Uri> Upload(
        string blobName,
        BinaryData content,
        string contentType,
        string cacheControl,
        CancellationToken cancellationToken
    )
    {
        var container = Container;
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new() { ContentType = contentType, CacheControl = cacheControl } },
            cancellationToken
        );
        return blob.Uri;
    }
}
