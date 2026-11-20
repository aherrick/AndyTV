using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace AndyTV.Watchlist.Services;

// Calls a model on the saved ChatGPT plan session. Every call refreshes the session first and saves the
// rotated refresh token; the lock keeps concurrent callers from spending the same refresh token twice.
public sealed class ChatGptClient(BlobStore blobStore, HttpClient http, ILogger<ChatGptClient> logger)
{
    private const string SessionBlob = "chatgpt-session.json";
    private const string Model = "gpt-6.1-sol";

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    // Web defaults accept numbers as strings, which would make the schema say "string or integer".
    private static readonly JsonSerializerOptions SchemaOptions = new(JsonSerializerOptions.Web)
    {
        NumberHandling = JsonNumberHandling.Strict,
    };

    // Web search on, response constrained to T's JSON schema.
    public async Task<T> Ask<T>(string prompt, CancellationToken cancellationToken = default)
    {
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(
            SchemaOptions,
            typeof(T),
            new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true }
        );
        var text = await Send(
            // Web search otherwise puts markdown citation links inside the text fields.
            prompt + "\n\nPut links only in the sources fields; never put citations, URLs or markdown inside any other field.",
            schema,
            cancellationToken
        );
        try
        {
            return JsonSerializer.Deserialize<T>(text, JsonSerializerOptions.Web)!;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("The model returned invalid JSON.", ex);
        }
    }

    private async Task<string> Send(string prompt, JsonNode schema, CancellationToken cancellationToken)
    {
        var accessToken = await RefreshAccessToken(cancellationToken);
        logger.LogInformation("ChatGPT session refreshed, sending request ({chars:N0} chars).", prompt.Length);

        // The API requires a streamed request here, with the same effort as the Copilot research path.
        var options = new CreateResponseOptions
        {
            Model = Model,
            StoredOutputEnabled = false,
            StreamingEnabled = true,
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.High,
            },
            Tools = { ResponseTool.CreateWebSearchTool() },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "response",
                    BinaryData.FromString(schema.ToJsonString()),
                    jsonSchemaIsStrict: false
                ),
            },
            InputItems = { ResponseItem.CreateUserMessageItem(prompt) },
        };

        var client = new ResponsesClient(
            new ApiKeyCredential(accessToken),
            new ResponsesClientOptions { NetworkTimeout = TimeSpan.FromMinutes(45) }
        );
        // The final event's output is empty when tools run, so the text is collected from the deltas.
        var text = new StringBuilder();
        var started = Stopwatch.GetTimestamp();
        var searches = 0;
        await foreach (var update in await client.CreateResponseStreamingAsync(options, cancellationToken))
        {
            if (update is StreamingResponseOutputItemDoneUpdate { Item: WebSearchCallResponseItem search })
            {
                // The typed item doesn't expose the query, so it's read from its JSON.
                var action = JsonNode.Parse(ModelReaderWriter.Write(search).ToString())!["action"];
                logger.LogInformation(
                    "ChatGPT web search #{n} [{elapsed}]: {query}",
                    ++searches,
                    Stopwatch.GetElapsedTime(started).ToString(@"mm\:ss"),
                    (string)action?["query"] ?? (string)action?["url"] ?? action?.ToJsonString()
                );
            }
            else if (update is StreamingResponseOutputTextDeltaUpdate delta)
            {
                if (text.Length == 0)
                {
                    logger.LogInformation(
                        "ChatGPT is writing the response [{elapsed}]",
                        Stopwatch.GetElapsedTime(started).ToString(@"mm\:ss")
                    );
                }
                text.Append(delta.Delta);
            }
            else if (update is StreamingResponseCompletedUpdate completed)
            {
                if (completed.Response.Usage is { } usage)
                {
                    logger.LogInformation(
                        "ChatGPT usage: {input:N0} in ({cached:N0} cached) / {output:N0} out ({reasoning:N0} reasoning).",
                        usage.InputTokenCount,
                        usage.InputTokenDetails?.CachedTokenCount,
                        usage.OutputTokenCount,
                        usage.OutputTokenDetails?.ReasoningTokenCount
                    );
                }
                return text.ToString();
            }
            else if (update is StreamingResponseFailedUpdate failed)
            {
                throw new InvalidOperationException(
                    $"Response failed: {failed.Response.Error?.Code} {failed.Response.Error?.Message}"
                );
            }
            else if (update is StreamingResponseIncompleteUpdate incomplete)
            {
                throw new InvalidOperationException(
                    $"Response incomplete: {incomplete.Response.IncompleteStatusDetails?.Reason}"
                );
            }
            else if (update is StreamingResponseErrorUpdate error)
            {
                throw new InvalidOperationException($"Stream error: {error.Code} {error.Message}");
            }
        }

        throw new InvalidOperationException(
            $"Stream ended without response.completed after {Stopwatch.GetElapsedTime(started):mm\\:ss} "
                + $"({searches} searches, {text.Length:N0} chars of text received)."
        );
    }

    // Refreshes the session and returns a fresh access token.
    public async Task<string> RefreshAccessToken(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
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
            return accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
