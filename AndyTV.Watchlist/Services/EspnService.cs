using System.Net.Http.Json;
using System.Text.Json;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

// Racing and PGA Tour golf from ESPN's public scoreboards (no key).
public sealed class EspnService(HttpClient httpClient, ILogger<EspnService> logger)
{
    private const string BaseUrl = "https://site.api.espn.com/apis/site/v2/sports/";
    private const string GolfUrl = BaseUrl + "golf/pga/scoreboard";

    // ESPN scoreboard slugs and the session types worth watching for each series.
    private static readonly (string Slug, string League, string[] Sessions)[] Series =
    [
        ("nascar-premier", "NASCAR Cup Series", ["Race"]),
        ("irl", "IndyCar Series", ["Race"]),
        ("f1", "Formula 1", ["Race", "Qual", "Sprint"]),
    ];

    // ESPN's Akamai front end rejects requests that don't look like a browser.
    internal static readonly (string Name, string Value)[] BrowserHeaders =
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
            [
                .. Series.Select(series =>
                    LoadRacing(series.Slug, series.League, series.Sessions, date, cancellationToken)
                ),
                LoadGolf(date, cancellationToken),
            ]
        );
        return [.. feeds.SelectMany(events => events)];
    }

    // Null on failure; these feeds are optional, so the rest of the slate continues.
    private async Task<JsonDocument> Fetch(string url, string league, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var (name, value) in BrowserHeaders)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogWarning(ex, "{league} feed request failed.", league);
            return null;
        }
    }

    private async Task<List<SportsEvent>> LoadGolf(DateOnly date, CancellationToken cancellationToken)
    {
        using var document = await Fetch(GolfUrl, "PGA Tour", cancellationToken);
        if (document is null)
        {
            return [];
        }

        List<SportsEvent> events = [];
        foreach (var tournament in document.RootElement.GetProperty("events").EnumerateArray())
        {
            var name = tournament.GetProperty("name").GetString();
            var start = EasternDate(tournament.GetProperty("date"));
            var end = EasternDate(tournament.GetProperty("endDate"));
            // Overseas play can begin the prior ET evening, so widen a day.
            if (string.IsNullOrWhiteSpace(name) || date < start.AddDays(-1) || date > end)
            {
                continue;
            }

            if (
                tournament.GetProperty("status").GetProperty("type").GetProperty("name").GetString()
                is "STATUS_CANCELED" or "STATUS_POSTPONED"
            )
            {
                continue;
            }

            // ESPN's date is a midnight placeholder unless timeValid is true; null lets the model research it.
            var competition = tournament.GetProperty("competitions")[0];
            DateTimeOffset? startTime = null;
            if (competition.TryGetProperty("timeValid", out var timeValid) && timeValid.GetBoolean())
            {
                var time = EasternTimeZone.Convert(competition.GetProperty("date").GetDateTimeOffset());
                if (DateOnly.FromDateTime(time.DateTime) == date)
                {
                    startTime = time;
                }
            }

            events.Add(new("Golf", "PGA Tour", null, null, startTime, GolfUrl) { EventName = name });
        }
        return events;
    }

    private static DateOnly EasternDate(JsonElement value) =>
        DateOnly.FromDateTime(EasternTimeZone.Convert(value.GetDateTimeOffset()).DateTime);

    private async Task<List<SportsEvent>> LoadRacing(
        string slug,
        string league,
        string[] sessions,
        DateOnly date,
        CancellationToken cancellationToken
    )
    {
        var url = $"{BaseUrl}racing/{slug}/scoreboard";
        using var document = await Fetch(url, league, cancellationToken);
        if (document is null)
        {
            return [];
        }

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
