using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Responses;

namespace AndyTV.Watchlist.Services;

public sealed class SportsGuideService(AppSettings settings)
{
    // Pricing per 1M tokens (USD) by model; adjust to your deployment's rates.
    private static readonly Dictionary<string, ModelPricing> Pricing = new(StringComparer.OrdinalIgnoreCase)
    {
        ["5.6-terra"] = new(2.00m, 12.00m),
        ["5.6-sol"] = new(4.00m, 20.00m),
        ["5.5"] = new(5.00m, 30.00m),
        ["5.4"] = new(2.50m, 15.00m),
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly BinaryData ResponseSchema = BinaryData.FromString(
        """
        {
          "type": "object",
          "properties": {
            "rankedEvents": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "eventId": { "type": "integer" },
                  "reason": { "type": "string" },
                  "network": { "type": ["string", "null"] }
                },
                "required": ["eventId", "reason", "network"],
                "additionalProperties": false
              }
            },
            "watchPlan": { "type": "string" }
          },
          "required": ["rankedEvents", "watchPlan"],
          "additionalProperties": false
        }
        """
    );

    public async Task<AiSportsGuide> CreateGuideAsync(
        IReadOnlyList<SportsEvent> events,
        DateTimeOffset easternNow,
        CancellationToken cancellationToken = default
    )
    {
        var client = new AzureOpenAIClient(
            settings.AzureOpenAiEndpoint,
            new AzureKeyCredential(settings.AzureOpenAiApiKey)
        );

        var developerPrompt =
            $"Today is {easternNow:MMMM d, yyyy}. The current time is {easternNow:h:mm tt} Eastern Time.\n\n{settings.DeveloperPrompt}";

        var options = new CreateResponseOptions(
            settings.AzureOpenAiDeployment,
            [
                ResponseItem.CreateDeveloperMessageItem(developerPrompt),
                ResponseItem.CreateUserMessageItem($"VERIFIED EVENTS\n{SerializeEvents(events)}"),
            ]
        )
        {
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.Medium,
            },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    "AndyTvSportsGuide",
                    ResponseSchema,
                    jsonSchemaIsStrict: true
                ),
            },
        };

        options.Tools.Add(ResponseTool.CreateWebSearchTool());

        var response = await client
            .GetResponsesClient()
            .CreateResponseAsync(options, cancellationToken);

        LogCost(settings.AzureOpenAiDeployment, response.Value.Usage);

        var guide =
            JsonSerializer.Deserialize<AiSportsGuide>(response.Value.GetOutputText(), JsonOptions)
            ?? throw new InvalidOperationException("The AI returned an empty guide.");

        ValidateGuide(guide, events);
        return guide;
    }

    private static void LogCost(string model, ResponseTokenUsage usage)
    {
        var pricing = Pricing
            .FirstOrDefault(entry => model.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
            .Value;

        if (pricing is null)
        {
            Console.WriteLine(
                $"AI model: {model} | {usage.InputTokenCount} in + {usage.OutputTokenCount} out | price: unknown"
            );
            return;
        }

        var cost =
            (usage.InputTokenCount * pricing.InputPerMillion
                + usage.OutputTokenCount * pricing.OutputPerMillion)
            / 1_000_000m;

        Console.WriteLine(
            $"AI model: {model} | {usage.InputTokenCount} in + {usage.OutputTokenCount} out | ${cost:F4}"
        );
    }

    private static string SerializeEvents(IReadOnlyList<SportsEvent> events) =>
        JsonSerializer.Serialize(
            events.Select(
                (sportsEvent, index) =>
                    new
                    {
                        eventId = index,
                        sportsEvent.Sport,
                        sportsEvent.League,
                        sportsEvent.Matchup,
                        StartTime = sportsEvent.StartTimeEastern,
                    }
            )
        );

    private static void ValidateGuide(AiSportsGuide guide, IReadOnlyList<SportsEvent> events)
    {
        if (
            guide.RankedEvents.Count is < 1 or > 20
            || guide.RankedEvents.Any(rankedEvent =>
                rankedEvent.EventId < 0
                || rankedEvent.EventId >= events.Count
                || string.IsNullOrWhiteSpace(rankedEvent.Reason)
            )
            || guide.RankedEvents.Select(rankedEvent => rankedEvent.EventId).Distinct().Count()
                != guide.RankedEvents.Count
            || string.IsNullOrWhiteSpace(guide.WatchPlan)
        )
        {
            throw new InvalidOperationException(
                "The AI guide did not reference a valid unique set of events."
            );
        }
    }

    private sealed record ModelPricing(decimal InputPerMillion, decimal OutputPerMillion);
}
