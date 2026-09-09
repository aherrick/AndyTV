using System.Text;
using AndyTV.Watchlist.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AndyTV.Watchlist.Services;

// One storage account, two jobs: it hosts the static site ($web/index.html) and the
// public Instagram card images (andytv-watchlist container).
public sealed class BlobStore(AppSettings settings)
{
    private const string ImageContainer = "andytv-watchlist";

    // Uploads a card PNG to a public container and returns its blob URL.
    public async Task<Uri> UploadImage(
        string blobName,
        byte[] png,
        CancellationToken cancellationToken = default
    )
    {
        var container = new BlobContainerClient(settings.BlobConnectionString, ImageContainer);
        await container.CreateIfNotExistsAsync(
            PublicAccessType.Blob,
            cancellationToken: cancellationToken
        );

        var blob = container.GetBlobClient(blobName);
        await Upload(blob, png, "image/png", null, cancellationToken);
        return blob.Uri;
    }

    // Uploads index.html to the $web container and returns its blob URL.
    public async Task<string> PublishSite(
        string html,
        CancellationToken cancellationToken = default
    )
    {
        var container = new BlobContainerClient(settings.BlobConnectionString, "$web");
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = container.GetBlobClient("index.html");
        await Upload(blob, Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8", "no-cache", cancellationToken);
        return blob.Uri.ToString();
    }

    private static async Task Upload(
        BlobClient blob,
        byte[] content,
        string contentType,
        string? cacheControl,
        CancellationToken cancellationToken
    )
    {
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
    }
}
