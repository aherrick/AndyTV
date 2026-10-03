using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public sealed partial class ApiSportsService(HttpClient httpClient, AppSettings settings)
{
    private static readonly HashSet<int> SoccerLeagues =
    [
        // Major international tournaments
        1, // FIFA World Cup
        4, // UEFA European Championship
        9, // Copa America
        // International teams
        5, // UEFA Nations League
        10, // International Friendlies
        // Major European club tournaments
        2, // UEFA Champions League
        3, // UEFA Europa League
        848, // UEFA Conference League
        // England
        39, // Premier League
        40, // EFL Championship
        45, // FA Cup
        48, // EFL Cup / Carabao Cup
        // Major European top divisions
        140, // La Liga
        135, // Serie A
        78, // Bundesliga
        61, // Ligue 1
        88, // Eredivisie
        94, // Primeira Liga
        // North America
        253, // MLS
        262, // Liga MX
        772, // Leagues Cup
    ];

    private static readonly HashSet<int> BasketballLeagues =
    [
        12, // NBA
        13, // WNBA (listed as "NBA W")
        116, // NCAA
        284, // FIBA World Cup (Women)
    ];

    public async Task<List<SportsEvent>> GetEventsForDate(
        DateOnly date,
        CancellationToken cancellationToken = default
    )
    {
        var query =
            $"date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&timezone=America/New_York";
        var feeds = await Task.WhenAll(
            Load("Baseball", $"https://v1.baseball.api-sports.io/games?{query}", [1], date, cancellationToken), // MLB
            Load("Football", $"https://v1.american-football.api-sports.io/games?{query}", [1, 2], date, cancellationToken), // NFL and NCAA
            Load("Hockey", $"https://v1.hockey.api-sports.io/games?{query}", [57], date, cancellationToken), // NHL
            Load("Basketball", $"https://v1.basketball.api-sports.io/games?{query}", BasketballLeagues, date, cancellationToken),
            Load("Soccer", $"https://v3.football.api-sports.io/fixtures?{query}", SoccerLeagues, date, cancellationToken),
            LoadUfc($"https://v1.mma.api-sports.io/fights?{query}", cancellationToken)
        );
        return [.. feeds.SelectMany(events => events)];
    }

    private async Task<List<SportsEvent>> Load(
        string sport,
        string url,
        HashSet<int> leagues,
        DateOnly date,
        CancellationToken cancellationToken
    )
    {
        using var document = await Get(sport, url, cancellationToken);

        List<SportsEvent> events = [];
        foreach (var game in document.RootElement.GetProperty("response").EnumerateArray())
        {
            var league = game.GetProperty("league");
            if (!leagues.Contains(league.GetProperty("id").GetInt32()))
            {
                continue;
            }

            var timestamp = sport switch
            {
                "Football" => game.GetProperty("game").GetProperty("date").GetProperty("timestamp").GetInt64(),
                "Soccer" => game.GetProperty("fixture").GetProperty("timestamp").GetInt64(),
                _ => game.GetProperty("timestamp").GetInt64(),
            };
            if (timestamp <= 0)
            {
                continue;
            }

            var start = EasternTimeZone.Convert(DateTimeOffset.FromUnixTimeSeconds(timestamp));
            if (DateOnly.FromDateTime(start.DateTime) != date)
            {
                continue;
            }

            var teams = game.GetProperty("teams");
            var home = teams.GetProperty("home").GetProperty("name").GetString();
            var away = teams.GetProperty("away").GetProperty("name").GetString();
            var name = league.GetProperty("name").GetString();
            if (
                !string.IsNullOrWhiteSpace(home)
                && !string.IsNullOrWhiteSpace(away)
                && !string.IsNullOrWhiteSpace(name)
                && !YouthTeam().IsMatch(home)
                && !YouthTeam().IsMatch(away)
            )
            {
                events.Add(new(sport, name, home, away, start, url));
            }
        }
        return events;
    }

    // One event per UFC card; bout times don't give a reliable main-card start, so the model researches it.
    private async Task<List<SportsEvent>> LoadUfc(string url, CancellationToken cancellationToken)
    {
        using var document = await Get("MMA", url, cancellationToken);

        return
        [
            .. document
                .RootElement.GetProperty("response")
                .EnumerateArray()
                .Select(fight => fight.GetProperty("slug").GetString())
                .Where(card => card?.StartsWith("UFC", StringComparison.OrdinalIgnoreCase) == true)
                .Distinct()
                .Select(card => new SportsEvent("MMA", "UFC", null, null, null, url) { EventName = card }),
        ];
    }

    private async Task<JsonDocument> Get(string sport, string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-apisports-key", settings.SportsApiKey);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var document =
            await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
            ?? throw new InvalidOperationException("Sports API returned an empty response.");
        var errors = document.RootElement.GetProperty("errors");
        if (
            (errors.ValueKind == JsonValueKind.Object && errors.EnumerateObject().Any())
            || (errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
        )
        {
            var message = $"{sport} API error: {errors}";
            document.Dispose();
            throw new InvalidOperationException(message);
        }
        return document;
    }

    [GeneratedRegex(@"\bU\d{2}$")]
    private static partial Regex YouthTeam();
}
