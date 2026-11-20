using System.Text.Json.Nodes;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace AndyTV.Watchlist;

/// <summary>
/// Every 30 minutes rotates the saved Sign in with ChatGPT session, then makes one
/// small Responses call. Refresh tokens last 30 days and each refresh issues a new one, so the session
/// lives in the private blob container instead of app settings.
/// </summary>
public sealed class CodexJobFn(BlobStore blobStore, HttpClient http, ILogger<CodexJobFn> logger)
{
    private const string SessionBlob = "chatgpt-session.json";

    [Function(nameof(CodexJobFn))]
    public async Task Run(
        [TimerTrigger("0 */30 * * * *")] TimerInfo _,
        CancellationToken cancellationToken
    )
    {
        var json =
            await blobStore.ReadPrivate(SessionBlob, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Seed {SessionBlob} in the private container first."
            );
        var session = JsonNode.Parse(json)!.AsObject();

        using var refresh = await http.PostAsync(
            "https://auth.openai.com/api/accounts/oauth/token",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = (string)session["client_id"]!,
                    ["refresh_token"] = (string)session["refresh_token"]!,
                    ["resource"] = "https://api.openai.com/v1",
                }
            ),
            cancellationToken
        );
        refresh.EnsureSuccessStatusCode();
        var tokens = JsonNode.Parse(await refresh.Content.ReadAsStringAsync(cancellationToken))!;

        // Save before using: the old refresh token is spent once the new one is issued.
        var accessToken = (string)tokens["access_token"]!;
        session["access_token"] = accessToken;
        session["refresh_token"] = (string)tokens["refresh_token"]!;
        await blobStore.WritePrivate(SessionBlob, session.ToJsonString(), cancellationToken);

        // The API requires a streamed request here; the final event carries the whole response.
        var options = new CreateResponseOptions
        {
            Model = "gpt-6.1-sol",
            StoredOutputEnabled = false,
            StreamingEnabled = true,
            InputItems = { ResponseItem.CreateUserMessageItem("Say hello from my Azure Function.") },
        };

        var client = new ResponsesClient(accessToken);
        await foreach (var update in await client.CreateResponseStreamingAsync(options, cancellationToken))
        {
            if (update is StreamingResponseCompletedUpdate completed)
            {
                logger.LogInformation("Response: {text}", completed.Response.GetOutputText());
                return;
            }
        }

        throw new InvalidOperationException("Stream ended without response.completed.");
    }
}
