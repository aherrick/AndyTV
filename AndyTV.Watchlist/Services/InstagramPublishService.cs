using System.Net.Http.Json;
using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

public sealed class InstagramPublishService(
    HttpClient httpClient,
    BlobStore blobStore,
    AppSettings settings,
    ILogger<InstagramPublishService> logger
)
{
    // Instagram Login tokens (IGAA...) only work on graph.instagram.com, not graph.facebook.com.
    private const string GraphBase = "https://graph.instagram.com/v21.0";

    // Publishes the given image URLs as a single Instagram carousel and returns the post's permalink.
    public async Task<string> PublishCarousel(
        IEnumerable<Uri> imageUrls,
        string caption,
        CancellationToken cancellationToken = default
    )
    {
        var token = await AccessToken(cancellationToken);

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
                    token,
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
            token,
            cancellationToken
        );

        var mediaId = await Post(
            $"{settings.InstagramUserId}/media_publish",
            new() { ["creation_id"] = carouselId },
            token,
            cancellationToken
        );

        var media = await httpClient.GetFromJsonAsync<JsonElement>(
            $"{GraphBase}/{mediaId}?fields=permalink&access_token={token}",
            cancellationToken
        );
        return media.GetProperty("permalink").GetString()!;
    }

    // Tokens expire after 60 days, so refresh on each post and keep the newest privately.
    // Delete the blob after setting a new INSTAGRAM_ACCESS_TOKEN.
    private async Task<string> AccessToken(CancellationToken cancellationToken)
    {
        var token = await blobStore.ReadPrivate("instagram-token.txt", cancellationToken) ?? settings.InstagramAccessToken!;
        using var response = await httpClient.GetAsync(
            $"https://graph.instagram.com/refresh_access_token?grant_type=ig_refresh_token&access_token={token}",
            cancellationToken
        );
        if (!response.IsSuccessStatusCode)
        {
            // Refresh is rejected until the token is 24h old; the current one is still valid.
            logger.LogInformation("Instagram token not refreshed ({status}).", (int)response.StatusCode);
            return token;
        }

        var refreshed = (await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken))
            .GetProperty("access_token").GetString()!;
        await blobStore.WritePrivate("instagram-token.txt", refreshed, cancellationToken);
        return refreshed;
    }

    private Task<string> CreateContainer(
        Dictionary<string, string> fields,
        string token,
        CancellationToken cancellationToken
    ) => Post($"{settings.InstagramUserId}/media", fields, token, cancellationToken);

    private async Task<string> Post(
        string endpoint,
        Dictionary<string, string> fields,
        string token,
        CancellationToken cancellationToken
    )
    {
        fields["access_token"] = token;

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
