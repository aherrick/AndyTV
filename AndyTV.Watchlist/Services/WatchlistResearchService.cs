using System.Diagnostics;
using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using GitHub.Copilot;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

// Loads feed events and has Copilot rank them into a DailyWatchlist.
public sealed class WatchlistResearchService(
    ApiSportsService apiSports,
    EspnRacingService racing,
    PgaTourService golf,
    ActionNetworkOddsService oddsService,
    AppSettings settings,
    ILogger<WatchlistResearchService> logger
)
{
    private const string ModelId = "gpt-6.1-sol";

    public async Task<List<SportsEvent>> LoadEvents(
        DateOnly[] days,
        CancellationToken cancellationToken
    )
    {
        var feeds = await Task.WhenAll(
            days.SelectMany(day =>
                new[]
                {
                    apiSports.GetEventsForDate(day, cancellationToken),
                    racing.GetEventsForDate(day, cancellationToken),
                    golf.GetEventsForDate(day, cancellationToken),
                }
            )
        );
        return [.. feeds.SelectMany(feed => feed).Distinct().OrderBy(e => e.StartTimeIso)];
    }

    // Returns the ranked watchlist and the research cost in dollars.
    public async Task<(DailyWatchlist Watchlist, double Cost)> Research(
        WatchlistKind kind,
        DateOnly[] days,
        List<SportsEvent> events,
        CancellationToken cancellationToken
    )
    {
        var odds = await oddsService.GetOdds(days, cancellationToken);
        logger.LogInformation("{kind} supplied {events} events and {count} FanDuel odds rows.", kind, events.Count, odds.Count);
        var prompt = WatchlistPrompt.Build(settings.WatchlistPrompt, days, events, odds);
        await using var client = new CopilotClient(
            new CopilotClientOptions { GitHubToken = settings.CopilotGitHubToken }
        );
        await client.StartAsync(cancellationToken);
        await using var session = await client.CreateSessionAsync(
            new SessionConfig
            {
                Model = ModelId,
                ReasoningEffort = "high",
                AvailableTools = ["web_search", "web_fetch"],
                OnPermissionRequest = PermissionHandler.ApproveAll,
            },
            cancellationToken
        );

        var stopwatch = Stopwatch.StartNew();
        var searches = 0;
        var fetches = 0;
        long inputTokens = 0;
        long outputTokens = 0;
        long nanoAiu = 0;
        using var subscription = session.On<SessionEvent>(evt =>
        {
            if (evt is ToolExecutionStartEvent tool)
            {
                logger.LogInformation(
                    "[{kind} {elapsed}] {tool} {args}",
                    kind,
                    stopwatch.Elapsed.ToString(@"mm\:ss"),
                    tool.Data.ToolName,
                    JsonSerializer.Serialize(tool.Data.Arguments)
                );
                if (tool.Data.ToolName == "web_search")
                {
                    searches++;
                }
                else if (tool.Data.ToolName == "web_fetch")
                {
                    fetches++;
                }
            }
            else if (evt is AssistantUsageEvent usage)
            {
                inputTokens += (long)(usage.Data.InputTokens ?? 0);
                outputTokens += (long)(usage.Data.OutputTokens ?? 0);
                nanoAiu += (long)(usage.Data.CopilotUsage?.TotalNanoAiu ?? 0);
            }
        });

        // SendAndWaitAsync<T> infers a strict JSON schema from DailyWatchlist.
        var result = await session.SendAndWaitAsync<DailyWatchlist>(
            prompt,
            timeout: TimeSpan.FromMinutes(45),
            cancellationToken: cancellationToken
        );

        var credits = nanoAiu / 1e9;
        var cost = credits * 0.01;
        logger.LogInformation(
            "{kind} research done in {elapsed}: {searches} searches, {fetches} fetches, {input:N0} in / {output:N0} out tokens, {credits:N2} AI credits (~${dollars:N2}).",
            kind,
            stopwatch.Elapsed.ToString(@"mm\:ss"),
            searches,
            fetches,
            inputTokens,
            outputTokens,
            credits,
            cost
        );

        // Every formatter (and SportsFormat.TopPicks) relies on rank order.
        result.BestWatches.Sort((a, b) => a.Rank.CompareTo(b.Rank));

        return (result, cost);
    }
}