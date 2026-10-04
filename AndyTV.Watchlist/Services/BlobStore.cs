using System.Text;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AndyTV.Watchlist.Services;

// One public container hosts latest.json, latest_weekend.json, and Instagram cards.
public sealed class BlobStore(AppSettings settings)
{
    private BlobContainerClient Container => new(settings.BlobConnectionString, "andytv-watchlist");

    // Never public: holds state such as the refreshed Instagram token.
    private BlobContainerClient PrivateContainer => new(settings.BlobConnectionString, "andytv-watchlist-private");

    public async Task<string?> ReadPrivate(string blobName, CancellationToken cancellationToken = default)
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

    // Uploads a card PNG to a public container and returns its blob URL.
    public Task<Uri> UploadImage(
        string blobName,
        byte[] png,
        CancellationToken cancellationToken = default
    ) => Upload(blobName, png, "image/png", null, cancellationToken);

    // Uploads a watchlist feed to the container root and returns its blob URL.
    public Task<Uri> PublishData(
        string json,
        WatchlistKind kind,
        CancellationToken cancellationToken = default
    ) => Upload(
        kind.FeedFileName(),
        Encoding.UTF8.GetBytes(json),
        "application/json; charset=utf-8",
        "no-cache",
        cancellationToken
    );

    // Sunday expires only the weekend feed. No container creation or deletion is
    // needed here, and an already-missing blob is a successful no-op.
    public Task DeleteWeekendData(CancellationToken cancellationToken = default) =>
        Container.GetBlobClient(WatchlistKind.Weekend.FeedFileName())
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);

    private async Task<Uri> Upload(
        string blobName,
        byte[] content,
        string contentType,
        string? cacheControl,
        CancellationToken cancellationToken
    )
    {
        var container = Container;
        await container.CreateIfNotExistsAsync(
            PublicAccessType.Blob,
            cancellationToken: cancellationToken
        );

        var blob = container.GetBlobClient(blobName);
        await using var stream = new MemoryStream(content);
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    CacheControl = cacheControl,
                },
            },
            cancellationToken
        );
        return blob.Uri;
    }
}
