using System.Text;
using AndyTV.Watchlist.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AndyTV.Watchlist.Services;

// One public container (andytv-watchlist) hosts both the daily data feed (latest.json) and
// the Instagram card images.
public sealed class BlobStore(AppSettings settings)
{
    private const string ImageContainer = "andytv-watchlist";

    // Uploads a card PNG to a public container and returns its blob URL.
    public Task<Uri> UploadImage(
        string blobName,
        byte[] png,
        CancellationToken cancellationToken = default
    ) => Upload(blobName, png, "image/png", null, cancellationToken);

    // Uploads latest.json to the container root and returns its blob URL.
    public async Task<string> PublishData(
        string json,
        CancellationToken cancellationToken = default
    )
    {
        var uri = await Upload(
            "latest.json",
            Encoding.UTF8.GetBytes(json),
            "application/json; charset=utf-8",
            "no-cache",
            cancellationToken
        );
        return uri.ToString();
    }

    private async Task<Uri> Upload(
        string blobName,
        byte[] content,
        string contentType,
        string? cacheControl,
        CancellationToken cancellationToken
    )
    {
        var container = new BlobContainerClient(settings.BlobConnectionString, ImageContainer);
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
