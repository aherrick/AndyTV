using System.Text;
using AndyTV.Watchlist.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AndyTV.Watchlist.Services;

// One storage account, two jobs: it hosts the static site ($web/index.html) and the
// public Instagram card images (andytv-watchlist container). When PublishLocal is set
// the site is written to a local publish/ folder instead of blob storage.
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

    // Publishes index.html and returns the destination (local path or blob URL).
    public async Task<string> PublishSite(
        string html,
        CancellationToken cancellationToken = default
    )
    {
        var bytes = Encoding.UTF8.GetBytes(html);

        if (settings.PublishLocal)
        {
            Directory.CreateDirectory(PublishFolder);
            var path = Path.Combine(PublishFolder, "index.html");
            await File.WriteAllBytesAsync(path, bytes, cancellationToken);
            return path;
        }

        var container = new BlobContainerClient(settings.BlobConnectionString, "$web");
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = container.GetBlobClient("index.html");
        await Upload(blob, bytes, "text/html; charset=utf-8", "no-cache", cancellationToken);
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

    // <repo root>/publish (repo root = the folder containing AndyTV.slnx).
    private static string PublishFolder
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AndyTV.slnx")))
            {
                dir = dir.Parent;
            }

            var root = dir?.FullName ?? Directory.GetCurrentDirectory();
            return Path.Combine(root, "publish");
        }
    }
}
