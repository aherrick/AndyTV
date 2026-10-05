using System.Net.Http.Json;
using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace AndyTV.Watchlist.Services;

public sealed class InstagramPublishService(
    HttpClient httpClient,
    BlobStore blobStore,
    AppSettings settings,
    ILogger<InstagramPublishService> logger
)
{
    // Instagram Login tokens (IGAA...) only work on graph.instagram.com, not graph.facebook.com.
    private const string GraphBase = "https://graph.instagram.com/v26.0";
    private const string TokenBlob = "instagram-token.txt";

    // Containers process asynchronously; publishing before FINISHED fails with "Media ID is not available".
    private static readonly ResiliencePipeline<string?> WhileInProgress = new ResiliencePipelineBuilder<string?>()
        .AddRetry(
            new RetryStrategyOptions<string?>
            {
                ShouldHandle = new PredicateBuilder<string?>().HandleResult("IN_PROGRESS"),
                MaxRetryAttempts = 30,
                Delay = TimeSpan.FromSeconds(5),
                BackoffType = DelayBackoffType.Constant,
            }
        )
        .Build();

    // Publishes the given image URLs as a single Instagram carousel and returns the post's permalink.
    public async Task<string> PublishCarousel(
        IEnumerable<Uri> imageUrls,
        CancellationToken cancellationToken = default
    )
    {
        var token = await AccessToken(cancellationToken);

        List<string> childIds = [];
        foreach (var imageUrl in imageUrls)
        {
            childIds.Add(
                await Post("media", new() { ["image_url"] = imageUrl.ToString(), ["is_carousel_item"] = "true" }, token, cancellationToken)
            );
        }

        var carouselId = await Post(
            "media",
            new() { ["media_type"] = "CAROUSEL", ["children"] = string.Join(',', childIds) },
            token,
            cancellationToken
        );
        await WaitUntilFinished(carouselId, token, cancellationToken);
        var mediaId = await Post("media_publish", new() { ["creation_id"] = carouselId }, token, cancellationToken);

        var media = await httpClient.GetFromJsonAsync<JsonElement>(
            $"{GraphBase}/{mediaId}?fields=permalink&access_token={token}",
            cancellationToken
        );
        return media.GetProperty("permalink").GetString()!;
    }

    private async Task WaitUntilFinished(string containerId, string token, CancellationToken cancellationToken)
    {
        var status = await WhileInProgress.ExecuteAsync(
            async ct =>
                (
                    await httpClient.GetFromJsonAsync<JsonElement>(
                        $"{GraphBase}/{containerId}?fields=status_code&access_token={token}",
                        ct
                    )
                ).GetProperty("status_code").GetString(),
            cancellationToken
        );
        if (status != "FINISHED")
        {
            throw new InvalidOperationException($"Instagram container {containerId} is {status}.");
        }
    }

    // Tokens expire after 60 days, so refresh on each post and keep the newest privately.
    // Delete the blob after setting a new INSTAGRAM_ACCESS_TOKEN.
    private async Task<string> AccessToken(CancellationToken cancellationToken)
    {
        var token = await blobStore.ReadPrivate(TokenBlob, cancellationToken) ?? settings.InstagramAccessToken!;
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
        await blobStore.WritePrivate(TokenBlob, refreshed, cancellationToken);
        return refreshed;
    }

    // POSTs to /{ig-user-id}/{edge} and returns the created id.
    private async Task<string> Post(
        string edge,
        Dictionary<string, string> fields,
        string token,
        CancellationToken cancellationToken
    )
    {
        fields["access_token"] = token;

        using var content = new FormUrlEncodedContent(fields);
        using var response = await httpClient.PostAsync(
            $"{GraphBase}/{settings.InstagramUserId}/{edge}",
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

        return JsonSerializer.Deserialize<JsonElement>(json).GetProperty("id").GetString()!;
    }
}
