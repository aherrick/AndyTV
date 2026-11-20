using System.Text.Json.Nodes;
using OpenAI.Responses;

namespace AndyTV.Watchlist.Services;

// Calls a model on the saved ChatGPT plan session. Each call refreshes the session first and saves
// the rotated refresh token, so callers must not run at the same time.
public sealed class ChatGptClient(BlobStore blobStore, HttpClient http)
{
    private const string SessionBlob = "chatgpt-session.json";

    public async Task<string> Ask(
        string prompt,
        string model = "gpt-6.1-sol",
        CancellationToken cancellationToken = default
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
            Model = model,
            StoredOutputEnabled = false,
            StreamingEnabled = true,
            InputItems = { ResponseItem.CreateUserMessageItem(prompt) },
        };

        var client = new ResponsesClient(accessToken);
        await foreach (var update in await client.CreateResponseStreamingAsync(options, cancellationToken))
        {
            if (update is StreamingResponseCompletedUpdate completed)
            {
                return completed.Response.GetOutputText();
            }
        }

        throw new InvalidOperationException("Stream ended without response.completed.");
    }
}
