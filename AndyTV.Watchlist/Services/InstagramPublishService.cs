using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

public sealed class InstagramPublishService(
    HttpClient httpClient,
    AppSettings settings,
    ILogger<InstagramPublishService> logger
)
{
    private const string GraphBase = "https://graph.facebook.com/v21.0";

    // Publishes the given image URLs as a single Instagram carousel and returns the published media id.
    public async Task<string> PublishCarousel(
        IEnumerable<Uri> imageUrls,
        string caption,
        CancellationToken cancellationToken = default
    )
    {
        var childIds = new List<string>();
        foreach (var imageUrl in imageUrls)
        {
            childIds.Add(
                await CreateContainer(
                    new()
                    {
                        ["image_url"] = imageUrl.ToString(),
                        ["is_carousel_item"] = "true",
                    },
                    cancellationToken
                )
            );
        }

        var carouselId = await CreateContainer(
            new()
            {
                ["media_type"] = "CAROUSEL",
                ["children"] = string.Join(',', childIds),
                ["caption"] = caption,
            },
            cancellationToken
        );

        var mediaId = await Post(
            $"{settings.InstagramUserId}/media_publish",
            new() { ["creation_id"] = carouselId },
            cancellationToken
        );

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Published Instagram carousel {mediaId}.", mediaId);
        }
        return mediaId;
    }

    private Task<string> CreateContainer(
        Dictionary<string, string> fields,
        CancellationToken cancellationToken
    ) => Post($"{settings.InstagramUserId}/media", fields, cancellationToken);

    private async Task<string> Post(
        string endpoint,
        Dictionary<string, string> fields,
        CancellationToken cancellationToken
    )
    {
        fields["access_token"] = settings.InstagramAccessToken!;

        using var content = new FormUrlEncodedContent(fields);
        using var response = await httpClient.PostAsync(
            $"{GraphBase}/{endpoint}",
            content,
            cancellationToken
        );

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Instagram API failed ({(int)response.StatusCode}): {json}"
            );
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Instagram API returned no id.");
    }
}
