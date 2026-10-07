using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

public sealed class PgaTourService(
    HttpClient httpClient,
    AppSettings settings,
    ILogger<PgaTourService> logger
)
{
    private const string Url = "https://api.balldontlie.io/pga/v2/tournaments?per_page=100&season=";

    // One event per PGA Tour tournament running on the date; no start time since the free tier has no tee times.
    public async Task<List<SportsEvent>> GetEventsForDate(
        DateOnly date,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(settings.BallDontLieApiKey))
        {
            return [];
        }

        JsonDocument document;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Url + date.Year);
            request.Headers.TryAddWithoutValidation("Authorization", settings.BallDontLieApiKey);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            document =
                await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
                ?? throw new JsonException("Empty response.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            // Golf is optional; a failure leaves the rest of the slate intact.
            logger.LogWarning(ex, "PGA Tour feed request failed.");
            return [];
        }

        using var _ = document;
        List<SportsEvent> events = [];
        foreach (var tournament in document.RootElement.GetProperty("data").EnumerateArray())
        {
            if (
                DateOnly.TryParse(
                    tournament.GetProperty("start_date").GetString(),
                    CultureInfo.InvariantCulture,
                    out var start
                )
                && DateOnly.TryParse(
                    tournament.GetProperty("end_date").GetString(),
                    CultureInfo.InvariantCulture,
                    out var end
                )
                // Dates are course-local with no tee times; widen a day so overseas play the prior ET evening isn't missed.
                && date >= start.AddDays(-1)
                && date <= end
                && tournament.GetProperty("status_state").GetString()
                    is not ("canceled" or "postponed" or "abandoned")
                && tournament.GetProperty("name").GetString() is { Length: > 0 } name
            )
            {
                events.Add(
                    new("Golf", "PGA Tour", null, null, null, Url + date.Year) { EventName = name }
                );
            }
        }
        return events;
    }
}