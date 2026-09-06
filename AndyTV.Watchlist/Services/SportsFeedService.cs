using System.Net.Http.Json;
using System.Text.Json;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

public abstract class SportsFeedService(HttpClient httpClient, ILogger logger)
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    protected HttpClient HttpClient { get; } = httpClient;

    protected ILogger Logger { get; } = logger;

    public abstract Task<IReadOnlyList<SportsEvent>> GetEventsForDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default
    );

    protected async Task<T?> GetJsonAsync<T>(
        string url,
        CancellationToken cancellationToken,
        params (string Name, string Value)[] headers
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var (name, value) in headers)
        {
            request.Headers.Add(name, value);
        }

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    protected void LogResult(string sport, bool kept, string detail)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "[{sport}] {status} | {detail}",
                sport,
                kept ? "KEEP" : "SKIP",
                detail
            );
        }
    }
}
