using System.Diagnostics;
using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using GitHub.Copilot;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

// Loads feed events and has Copilot rank them into a DailyWatchlist.
public sealed class WatchlistResearchService(
    SportsEventService sportsEvents,
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
        var feeds = await Task.WhenAll(days.Select(day => sportsEvents.GetEventsForDate(day, cancellationToken)));
        List<SportsEvent> events = [.. feeds.SelectMany(feed => feed).Distinct().OrderBy(e => e.StartTimeIso)];
        logger.LogInformation("Loaded {count} events for {days}.", events.Count, string.Join(", ", days));
        foreach (var e in events)
        {
            logger.LogInformation("Event: {event}", SportsFormat.EventLine(e));
        }
        return events;
    }

    // Returns the ranked watchlist and the research cost in dollars.
    public async Task<(DailyWatchlist Watchlist, double Cost)> Research(
        WatchlistKind kind,
        DateOnly[] days,
        List<SportsEvent> events,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("{kind} supplied {events} events, {odds} with odds.", kind, events.Count, events.Count(e => e.Betting is not null));
        var prompt = WatchlistPrompt.Build(settings.WatchlistPrompt, days, events);
        var cost = 0.0;
        try
        {
            return (await ResearchOnce(kind, prompt, attemptCost => cost += attemptCost, cancellationToken), cost);
        }
        // Copilot occasionally fails a session server-side; a fresh session usually succeeds.
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("{kind} research failed (~${cost:N2} spent), retrying once: {error}", kind, cost, ex.Message);
            return (await ResearchOnce(kind, prompt, attemptCost => cost += attemptCost, cancellationToken), cost);
        }
    }

    private async Task<DailyWatchlist> ResearchOnce(
        WatchlistKind kind,
        string prompt,
        Action<double> addCost,
        CancellationToken cancellationToken
    )
    {
        await using var client = new CopilotClient(
            new CopilotClientOptions { GitHubToken = settings.CopilotGitHubToken }
        );
        await client.StartAsync(cancellationToken);
        await using var session = await client.CreateSessionAsync(
            new SessionConfig
            {
                Model = ModelId,
                ReasoningEffort = "max",
                AvailableTools = ["web_search", "web_fetch"],
                OnPermissionRequest = PermissionHandler.ApproveAll,
            },
            cancellationToken
        );

        var stopwatch = Stopwatch.StartNew();
        var searches = 0;
        var fetches = 0;
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
        });

        try
        {
            // SendAndWaitAsync<T> infers a strict JSON schema from DailyWatchlist.
            var result = await session.SendAndWaitAsync<DailyWatchlist>(
                prompt,
                timeout: TimeSpan.FromMinutes(45),
                cancellationToken: cancellationToken
            );

            // Every formatter (and SportsFormat.TopPicks) relies on rank order.
            result.BestWatches.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            return result;
        }
        finally
        {
            // Also runs for a failed attempt so its credits still count toward the run cost.
            addCost(await LogUsage(kind, session, stopwatch.Elapsed, searches, fetches));
        }
    }

    // Logs the session-wide usage (all models and subagents) and returns its cost in dollars.
    private async Task<double> LogUsage(
        WatchlistKind kind,
        CopilotSession session,
        TimeSpan elapsed,
        int searches,
        int fetches
    )
    {
        UsageGetMetricsResult metrics;
        try
        {
            metrics = await session.Rpc.Usage.GetMetricsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning("{kind} usage metrics unavailable: {error}", kind, ex.Message);
            return 0;
        }

        foreach (var (model, metric) in metrics.ModelMetrics)
        {
            logger.LogInformation(
                "{kind} {model}: {requests} requests, {input:N0} in / {output:N0} out / {reasoning:N0} reasoning / {cacheRead:N0} cache-read tokens, {credits:N2} AI credits.",
                kind,
                model,
                metric.Requests.Count,
                metric.Usage.InputTokens,
                metric.Usage.OutputTokens,
                metric.Usage.ReasoningTokens ?? 0,
                metric.Usage.CacheReadTokens,
                (metric.TotalNanoAiu ?? 0) / 1e9
            );
        }

        var credits = (metrics.TotalNanoAiu ?? 0) / 1e9;
        var cost = credits * 0.01;
        logger.LogInformation(
            "{kind} research finished in {elapsed}: {searches} searches, {fetches} fetches, {apiTime} API time, {credits:N2} AI credits (~${dollars:N2}).",
            kind,
            elapsed.ToString(@"mm\:ss"),
            searches,
            fetches,
            metrics.TotalApiDuration.ToString(@"mm\:ss"),
            credits,
            cost
        );
        return cost;
    }
}