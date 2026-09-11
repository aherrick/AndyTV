using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AndyTV.Watchlist.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace AndyTV.Watchlist.Services;

public sealed class CloudflareScreenshotService(
    HttpClient httpClient,
    AppSettings settings,
    ILogger<CloudflareScreenshotService> logger
)
{
    // Retry Workers Free rate limits (1 Quick Action / 10s) and transient render stalls;
    // honor Retry-After, otherwise wait 11s. The inner timeout makes stalled renders retryable.
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline =
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(
                new RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(response =>
                            response.StatusCode == HttpStatusCode.TooManyRequests
                        )
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutRejectedException>(),
                    MaxRetryAttempts = 5,
                    DelayGenerator = args =>
                        ValueTask.FromResult<TimeSpan?>(
                            args.Outcome.Result?.Headers.RetryAfter?.Delta
                                ?? TimeSpan.FromSeconds(11)
                        ),
                    OnRetry = args =>
                    {
                        logger.LogWarning(
                            "Cloudflare screenshot retry {attempt} of 5.",
                            args.AttemptNumber + 1
                        );
                        return ValueTask.CompletedTask;
                    },
                }
            )
            // Per-attempt cap so a stalled render surfaces as a retryable timeout, not a hard cancellation.
            .AddTimeout(TimeSpan.FromSeconds(60))
            .Build();

    public async Task<byte[]> Capture(string html, CancellationToken cancellationToken = default)
    {
        var url =
            $"https://api.cloudflare.com/client/v4/accounts/{settings.CloudflareAccountId}/browser-rendering/screenshot";

        var payload = new
        {
            html,
            viewport = new { width = 1080, height = 1350 },
            screenshotOptions = new { fullPage = false, type = "png" },
        };

        using var response = await _pipeline.ExecuteAsync(
            async token =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(payload),
                };
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    settings.CloudflareApiToken
                );
                return await httpClient.SendAsync(request, token);
            },
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Cloudflare screenshot failed ({status}): {error}",
                (int)response.StatusCode,
                error
            );
            throw new InvalidOperationException(
                $"Cloudflare screenshot failed ({(int)response.StatusCode})."
            );
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}


