using System.Diagnostics;
using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using GitHub.Copilot;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

// Loads feed events and has the model rank them into a DailyWatchlist.
public sealed class WatchlistResearchService(
    SportsEventService sportsEvents,
    ChatGptClient chatGpt,
    AppSettings settings,
    ILogger<WatchlistResearchService> logger
)
{
    // Both connections run the same model at extra-high reasoning effort.
    private const string ModelId = "gpt-6.1-sol";

    // The SDK's default options, minus the [FromFeed] fields that AttachFeedData fills in.
    private static readonly JsonSerializerOptions CopilotOptions = FromFeedAttribute.OmitFrom(AIJsonUtilities.DefaultOptions);

    public async Task<List<SportsEvent>> LoadEvents(
        DateOnly[] days,
        CancellationToken cancellationToken
    )
    {
        var feeds = await Task.WhenAll(
            days.Select(day => sportsEvents.GetEventsForDate(day, cancellationToken))
        );
        List<SportsEvent> events =
        [
            .. feeds.SelectMany(feed => feed).Distinct().OrderBy(e => e.StartTimeIso),
        ];
        logger.LogInformation(
            "Loaded {count} events for {days}.",
            events.Count,
            string.Join(", ", days)
        );
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
        logger.LogInformation(
            "{kind} supplied {events} events, {odds} with odds.",
            kind,
            events.Count,
            events.Count(e => e.Betting is not null)
        );
        var prompt = WatchlistPrompt.Build(settings.WatchlistPrompt, days, events);

        // Daily uses the ChatGPT plan. Weekend researches more days, so it goes straight to Copilot:
        // the plan closes streams after 15 minutes and Copilot allows 45.
        var (watchlist, cost) =
            kind == WatchlistKind.Daily && await AskChatGpt(kind, prompt, cancellationToken) is { } answer
                ? (answer, 0.0)
                : await AskCopilot(kind, prompt, cancellationToken);

        // Every formatter (and SportsFormat.TopPicks) relies on rank order.
        watchlist.BestWatches.Sort((a, b) => a.Rank.CompareTo(b.Rank));
        AttachFeedData(watchlist, events);
        return (watchlist, cost);
    }

    // Odds and team abbreviations come from each pick's ESPN event, not the model. The prompt has the model copy
    // matchups exactly, so a pick finds its event by matchup; a doubleheader's games share one, so the nearest
    // start time decides. A pick that matches no event keeps no odds.
    private void AttachFeedData(DailyWatchlist watchlist, List<SportsEvent> events)
    {
        for (var i = 0; i < watchlist.BestWatches.Count; i++)
        {
            var game = watchlist.BestWatches[i];
            var match = events
                .Where(e => e.Matchup == game.Matchup)
                .MinBy(e => ((e.StartTimeIso ?? game.StartTimeIso) - game.StartTimeIso).Duration());
            if (match is null)
            {
                logger.LogWarning("Pick #{rank} {matchup} matches no feed event, so it has no odds.", game.Rank, game.Matchup);
                continue;
            }

            watchlist.BestWatches[i] = game with
            {
                Betting = match.Betting,
                AwayTeamAbbr = match.AwayAbbr,
                HomeTeamAbbr = match.HomeAbbr,
            };
        }
    }

    // Returns null on any failure (usage limit, the 15-minute stream cut, invalid JSON), so the caller
    // falls back to Copilot instead of spending another long attempt on the plan.
    private async Task<DailyWatchlist> AskChatGpt(
        WatchlistKind kind,
        string prompt,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("{kind} research running on ChatGPT.", kind);
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await chatGpt.Ask<DailyWatchlist>(ModelId, prompt, cancellationToken);
            logger.LogInformation(
                "{kind} research finished on ChatGPT in {elapsed} (no per-call cost).",
                kind,
                Stopwatch.GetElapsedTime(started).ToString(@"mm\:ss")
            );
            return result;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "{kind} research failed on ChatGPT after {elapsed}, falling back to Copilot: {error}",
                kind,
                Stopwatch.GetElapsedTime(started).ToString(@"mm\:ss"),
                ex.Message
            );
            return null;
        }
    }

    // Returns the watchlist and the cost in dollars of every attempt.
    private async Task<(DailyWatchlist Watchlist, double Cost)> AskCopilot(
        WatchlistKind kind,
        string prompt,
        CancellationToken cancellationToken
    )
    {
        var cost = 0.0;
        void AddCost(double attemptCost) => cost += attemptCost;
        try
        {
            return (await AskCopilotOnce(kind, prompt, AddCost, cancellationToken), cost);
        }
        // Copilot occasionally fails a session server-side; a fresh session usually succeeds.
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(
                "{kind} research failed on Copilot (~${cost:N2} spent), retrying once: {error}",
                kind,
                cost,
                ex.Message
            );
            return (await AskCopilotOnce(kind, prompt, AddCost, cancellationToken), cost);
        }
    }

    private async Task<DailyWatchlist> AskCopilotOnce(
        WatchlistKind kind,
        string prompt,
        Action<double> addCost,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("{kind} research running on Copilot.", kind);
        await using var client = new CopilotClient(
            new CopilotClientOptions { GitHubToken = settings.CopilotGitHubToken }
        );
        await client.StartAsync(cancellationToken);
        await using var session = await client.CreateSessionAsync(
            new SessionConfig
            {
                Model = ModelId,
                ReasoningEffort = "xhigh",
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
            return await session.SendAndWaitAsync<DailyWatchlist>(
                prompt,
                serializerOptions: CopilotOptions,
                timeout: TimeSpan.FromMinutes(45),
                cancellationToken: cancellationToken
            );
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

        var usage = metrics.ModelMetrics.Values.Select(metric => metric.Usage).ToList();
        var credits = (metrics.TotalNanoAiu ?? 0) / 1e9;
        var cost = credits * 0.01;
        logger.LogInformation(
            "{kind} research finished on Copilot in {elapsed}: {searches} searches, {fetches} fetches, {input:N0} in / {output:N0} out / {reasoning:N0} reasoning tokens, {credits:N2} AI credits (~${dollars:N2}).",
            kind,
            elapsed.ToString(@"mm\:ss"),
            searches,
            fetches,
            usage.Sum(u => u.InputTokens),
            usage.Sum(u => u.OutputTokens),
            usage.Sum(u => u.ReasoningTokens ?? 0),
            credits,
            cost
        );
        return cost;
    }
}