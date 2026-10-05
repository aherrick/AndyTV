using System.Net.Http.Json;
using System.Text.Json;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

public sealed class EspnRacingService(HttpClient httpClient, ILogger<EspnRacingService> logger)
{
    // ESPN scoreboard slugs and the session types worth watching for each series.
    private static readonly (string Slug, string League, string[] Sessions)[] Series =
    [
        ("nascar-premier", "NASCAR Cup Series", ["Race"]),
        ("irl", "IndyCar Series", ["Race"]),
        ("f1", "Formula 1", ["Race", "Qual", "Sprint"]),
    ];

    // ESPN's Akamai front end rejects requests that don't look like a browser.
    private static readonly (string Name, string Value)[] BrowserHeaders =
    [
        ("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36"),
        ("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"),
        ("Accept-Language", "en-US,en;q=0.9"),
        ("Sec-Fetch-Dest", "document"),
        ("Sec-Fetch-Mode", "navigate"),
        ("Sec-Fetch-Site", "none"),
        ("Sec-Fetch-User", "?1"),
        ("Upgrade-Insecure-Requests", "1"),
    ];

    public async Task<List<SportsEvent>> GetEventsForDate(
        DateOnly date,
        CancellationToken cancellationToken = default
    )
    {
        var feeds = await Task.WhenAll(
            Series.Select(series => Load(series.Slug, series.League, series.Sessions, date, cancellationToken))
        );
        return [.. feeds.SelectMany(events => events)];
    }

    private async Task<List<SportsEvent>> Load(
        string slug,
        string league,
        string[] sessions,
        DateOnly date,
        CancellationToken cancellationToken
    )
    {
        var url = $"https://site.api.espn.com/apis/site/v2/sports/racing/{slug}/scoreboard";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var (name, value) in BrowserHeaders)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        JsonDocument document;
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            document =
                await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
                ?? throw new InvalidOperationException($"Empty ESPN response for {league}.");
        }
        catch (HttpRequestException ex)
        {
            // ESPN can 403 datacenter IPs; racing is optional, so keep the rest of the slate.
            logger.LogWarning(ex, "{league} feed request failed.", league);
            return [];
        }

        using var _ = document;
        List<SportsEvent> events = [];
        foreach (var race in document.RootElement.GetProperty("events").EnumerateArray())
        {
            var name = race.GetProperty("name").GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            // F1's event date is Friday practice, so judge each session by its own start time.
            var competitions = race.GetProperty("competitions");
            foreach (var competition in competitions.EnumerateArray())
            {
                // NASCAR and IndyCar may expose a sole untyped race competition.
                var session =
                    competition.TryGetProperty("type", out var type)
                    && type.TryGetProperty("abbreviation", out var abbreviation)
                        ? abbreviation.GetString()
                        : competitions.GetArrayLength() == 1 ? "Race" : null;
                if (session is null || !sessions.Contains(session, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var start = EasternTimeZone.Convert(competition.GetProperty("date").GetDateTimeOffset());
                if (DateOnly.FromDateTime(start.DateTime) != date)
                {
                    continue;
                }

                var suffix = session.ToLowerInvariant() switch
                {
                    "qual" => " - Qualifying",
                    "sprint" => " - Sprint",
                    _ => "",
                };
                events.Add(new("Racing", league, null, null, start, url) { EventName = name + suffix });
            }
        }
        return events;
    }
}
