using System.Diagnostics;
using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using GitHub.Copilot;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

// Builds a watchlist end to end: feed events -> Copilot research -> validated DailyWatchlist.
public sealed class WatchlistResearchService(
    ApiSportsService apiSports,
    EspnRacingService racing,
    AppSettings settings,
    ILogger<WatchlistResearchService> logger
)
{
    private const string ModelId = "gpt-6.1-sol";

    // Returns null when the feeds have nothing to rank.
    public async Task<DailyWatchlist?> Create(
        WatchlistKind kind,
        DateOnly runDate,
        CancellationToken cancellationToken = default
    )
    {
        DateOnly[] days = kind == WatchlistKind.Weekend ? [runDate.AddDays(1), runDate.AddDays(2)] : [runDate];
        var feeds = await Task.WhenAll(
            days.SelectMany(day => new[]
            {
                apiSports.GetEventsForDate(day, cancellationToken),
                racing.GetEventsForDate(day, cancellationToken),
            })
        );
        var events = feeds.SelectMany(feed => feed).Distinct().OrderBy(e => e.StartTimeIso).ToList();
        logger.LogInformation("Loaded {count} {kind} events.", events.Count, kind);
        if (events.Count == 0)
        {
            return null;
        }

        var watchlist = await Research(kind, WatchlistPrompt.Build(settings.WatchlistPrompt, days, events), cancellationToken);

        // Every formatter (and SportsFormat.TopPicks) relies on rank order.
        watchlist.BestWatches.Sort((a, b) => a.Rank.CompareTo(b.Rank));
        return watchlist;
    }

    private async Task<DailyWatchlist> Research(WatchlistKind kind, string prompt, CancellationToken cancellationToken)
    {
        await using var client = new CopilotClient(
            new CopilotClientOptions { GitHubToken = settings.CopilotGitHubToken }
        );
        await client.StartAsync();
        await using var session = await client.CreateSessionAsync(
            new SessionConfig
            {
                Model = ModelId,
                ReasoningEffort = "high",
                AvailableTools = ["web_search", "web_fetch"],
                OnPermissionRequest = PermissionHandler.ApproveAll,
            }
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
                    Interlocked.Increment(ref searches);
                }
                else if (tool.Data.ToolName == "web_fetch")
                {
                    Interlocked.Increment(ref fetches);
                }
            }
            else if (evt is AssistantUsageEvent usage)
            {
                Interlocked.Add(ref inputTokens, (long)(usage.Data.InputTokens ?? 0));
                Interlocked.Add(ref outputTokens, (long)(usage.Data.OutputTokens ?? 0));
                Interlocked.Add(ref nanoAiu, (long)(usage.Data.CopilotUsage?.TotalNanoAiu ?? 0));
            }
        });

        // SendAndWaitAsync<T> infers a strict JSON schema from DailyWatchlist.
        var result = await session.SendAndWaitAsync<DailyWatchlist>(
            prompt,
            timeout: TimeSpan.FromMinutes(15),
            cancellationToken: cancellationToken
        );

        var credits = nanoAiu / 1e9;
        logger.LogInformation(
            "{kind} research done in {elapsed}: {searches} searches, {fetches} fetches, {input:N0} in / {output:N0} out tokens, {credits:N2} AI credits (~${dollars:N2}).",
            kind,
            stopwatch.Elapsed.ToString(@"mm\:ss"),
            searches,
            fetches,
            inputTokens,
            outputTokens,
            credits,
            credits * 0.01
        );
        return result;
    }
}
