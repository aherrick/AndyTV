using AndyTV.Watchlist.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AndyTV.Watchlist.Services;

public sealed class BlobImageStore(AppSettings settings)
{
    private const string ContainerName = "andytv-watchlist";

    // Uploads a PNG and returns its public blob URL.
    public async Task<Uri> Upload(
        string blobName,
        byte[] content,
        CancellationToken cancellationToken = default
    )
    {
        var container = new BlobContainerClient(settings.BlobConnectionString, ContainerName);
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
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/png" },
            },
            cancellationToken
        );

        return blob.Uri;
    }
}