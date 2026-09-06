using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

public sealed class EspnRacingService(HttpClient httpClient, ILogger<EspnRacingService> logger)
    : SportsFeedService(httpClient, logger)
{
    // ESPN public scoreboard slugs for the three U.S.-relevant series and their display names.
    private static readonly (string Slug, string League)[] Series =
    [
        ("nascar-premier", "NASCAR Cup Series"),
        ("irl", "IndyCar Series"),
        ("f1", "Formula 1"),
    ];

    public override async Task<IReadOnlyList<SportsEvent>> GetEventsForDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default
    )
    {
        var events = new List<SportsEvent>();
        foreach (var (slug, league) in Series)
        {
            events.AddRange(await LoadSeriesAsync(slug, league, targetDate, cancellationToken));
        }

        return events;
    }

    private async Task<List<SportsEvent>> LoadSeriesAsync(
        string slug,
        string league,
        DateOnly targetDate,
        CancellationToken cancellationToken
    )
    {
        var url = $"https://site.api.espn.com/apis/site/v2/sports/racing/{slug}/scoreboard";

        EspnScoreboardDto board;
        try
        {
            board =
                await GetJsonAsync<EspnScoreboardDto>(url, cancellationToken)
                ?? new EspnScoreboardDto();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "[Racing] {league} feed request failed.", league);
            return [];
        }

        var events = new List<SportsEvent>();
        foreach (var race in board.Events)
        {
            // NASCAR/IndyCar expose a single competition (the race); F1 lists practice/qualifying/race,
            // so prefer the "Race" competition's start time and fall back to the sole/event date.
            var raceComp =
                race.Competitions.Find(competition =>
                    competition.Type.Abbreviation.Equals("Race", StringComparison.OrdinalIgnoreCase)
                ) ?? race.Competitions.FirstOrDefault();

            var startEastern = EasternTimeZone.Convert(raceComp?.Date ?? race.Date);

            var kept =
                DateOnly.FromDateTime(startEastern.DateTime) == targetDate
                && !string.IsNullOrWhiteSpace(race.Name);

            if (kept)
            {
                events.Add(new SportsEvent("Racing", league, race.Name, "", startEastern));
            }

            LogResult("Racing", kept, $"{league} | {race.Name} | {startEastern}");
        }

        return events;
    }
}