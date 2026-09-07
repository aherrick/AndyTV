using System.Globalization;
using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

public sealed class ApiSportsService : SportsFeedService
{
    private static readonly HashSet<int> TopSoccerLeagueIds =
    [
        // Major international tournaments
        1, // FIFA World Cup
        4, // UEFA European Championship (Euros)
        9, // Copa America
        // Major European club tournaments
        2, // UEFA Champions League
        3, // UEFA Europa League
        848, // UEFA Conference League
        // England
        39, // Premier League
        40, // EFL Championship
        41, // EFL League One
        45, // FA Cup
        48, // EFL Cup / Carabao Cup
        // Major European top divisions
        140, // La Liga - Spain
        135, // Serie A - Italy
        78, // Bundesliga - Germany
        61, // Ligue 1 - France
        88, // Eredivisie - Netherlands
        94, // Primeira Liga - Portugal
        // North America
        253, // MLS
        262, // Liga MX
        772, // Leagues Cup
    ];

    private static readonly HashSet<int> TopBasketballLeagueIds =
    [
        12, // NBA
        116, // NCAA
        284, // FIBA World Cup (Women)
    ];

    private readonly string _apiKey;

    public ApiSportsService(
        HttpClient httpClient,
        AppSettings settings,
        ILogger<ApiSportsService> logger
    )
        : base(httpClient, logger)
    {
        _apiKey = settings.SportsApiKey;
    }

    public override async Task<IReadOnlyList<SportsEvent>> GetEventsForDateAsync(
        DateOnly targetDate,
        CancellationToken cancellationToken = default
    )
    {
        var events = new List<SportsEvent>();
        events.AddRange(await GetBaseballAsync(targetDate, cancellationToken));
        events.AddRange(await GetFootballAsync(targetDate, cancellationToken));
        events.AddRange(await GetHockeyAsync(targetDate, cancellationToken));
        events.AddRange(await GetBasketballAsync(targetDate, cancellationToken));
        events.AddRange(await GetSoccerAsync(targetDate, cancellationToken));

        return events
            .Where(sportsEvent =>
                DateOnly.FromDateTime(sportsEvent.StartTimeEastern.DateTime) == targetDate
                && sportsEvent.StartTimeEastern.TimeOfDay >= TimeSpan.FromHours(7)
            )
            .OrderBy(sportsEvent => sportsEvent.StartTimeEastern)
            .ToList();
    }

    private Task<IReadOnlyList<SportsEvent>> GetBaseballAsync(
        DateOnly date,
        CancellationToken cancellationToken
    ) =>
        LoadEventsAsync<BaseballGameDto>(
            "Baseball",
            $"https://v1.baseball.api-sports.io/games?{DateQuery(date)}",
            game =>
                game.League.Id == 1
                    ? ToSportsEvent("Baseball", game.League.Name, game.Teams, game.Timestamp)
                    : null,
            cancellationToken
        );

    private Task<IReadOnlyList<SportsEvent>> GetFootballAsync(
        DateOnly date,
        CancellationToken cancellationToken
    ) =>
        LoadEventsAsync<FootballGameDto>(
            "Football",
            $"https://v1.american-football.api-sports.io/games?{DateQuery(date)}",
            game =>
                game.League.Id is 1 or 2
                    ? ToSportsEvent(
                        "Football",
                        game.League.Name,
                        game.Teams,
                        game.Game.Date.Timestamp
                    )
                    : null,
            cancellationToken
        );

    private Task<IReadOnlyList<SportsEvent>> GetHockeyAsync(
        DateOnly date,
        CancellationToken cancellationToken
    ) =>
        LoadEventsAsync<HockeyGameDto>(
            "Hockey",
            $"https://v1.hockey.api-sports.io/games?{DateQuery(date)}",
            game =>
                game.League.Id == 57
                    ? ToSportsEvent("Hockey", game.League.Name, game.Teams, game.Timestamp)
                    : null,
            cancellationToken
        );

    private Task<IReadOnlyList<SportsEvent>> GetBasketballAsync(
        DateOnly date,
        CancellationToken cancellationToken
    ) =>
        LoadEventsAsync<BasketballGameDto>(
            "Basketball",
            $"https://v1.basketball.api-sports.io/games?{DateQuery(date)}",
            game =>
                TopBasketballLeagueIds.Contains(game.League.Id)
                    ? ToSportsEvent("Basketball", game.League.Name, game.Teams, game.Timestamp)
                    : null,
            cancellationToken
        );

    private Task<IReadOnlyList<SportsEvent>> GetSoccerAsync(
        DateOnly date,
        CancellationToken cancellationToken
    ) =>
        LoadEventsAsync<SoccerFixtureDto>(
            "Soccer",
            $"https://v3.football.api-sports.io/fixtures?{DateQuery(date)}",
            fixture =>
                TopSoccerLeagueIds.Contains(fixture.League.Id)
                    ? ToSportsEvent(
                        "Soccer",
                        fixture.League.Name,
                        fixture.Teams,
                        fixture.Fixture.Timestamp
                    )
                    : null,
            cancellationToken
        );

    private async Task<IReadOnlyList<SportsEvent>> LoadEventsAsync<T>(
        string sport,
        string url,
        Func<T, SportsEvent?> map,
        CancellationToken cancellationToken
    )
        where T : IGameDto
    {
        var response =
            await GetJsonAsync<ApiSportsResponse<T>>(
                url,
                cancellationToken,
                ("x-apisports-key", _apiKey)
            ) ?? throw new InvalidOperationException("Sports API returned an empty response.");

        if (
            response.Errors.ValueKind == JsonValueKind.Object
            && response.Errors.EnumerateObject().Any()
        )
        {
            throw new InvalidOperationException($"Sports API error: {response.Errors}");
        }

        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "[{sport}] API returned {count} raw game(s).",
                sport,
                response.Response.Count
            );
        }

        var events = new List<SportsEvent>();
        foreach (var game in response.Response)
        {
            var mapped = map(game);
            if (mapped is not null)
            {
                events.Add(mapped);
            }

            LogResult(
                sport,
                mapped is not null,
                $"league {game.League.Id} {game.League.Name} | {game.Teams.Away.Name} @ {game.Teams.Home.Name}"
            );
        }

        return events;
    }

    private static SportsEvent? ToSportsEvent(
        string sport,
        string league,
        TeamsDto teams,
        long unixTimestamp
    )
    {
        if (
            unixTimestamp <= 0
            || string.IsNullOrWhiteSpace(league)
            || string.IsNullOrWhiteSpace(teams.Home.Name)
            || string.IsNullOrWhiteSpace(teams.Away.Name)
        )
        {
            return null;
        }

        var startTimeEastern = EasternTimeZone.Convert(
            DateTimeOffset.FromUnixTimeSeconds(unixTimestamp)
        );

        return new SportsEvent(sport, league, teams.Home.Name, teams.Away.Name, startTimeEastern);
    }

    private static string DateQuery(DateOnly date) =>
        $"date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&timezone=America/New_York";
}