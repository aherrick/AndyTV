using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

// Every sport in the watchlist, from ESPN's public scoreboards (no key).
public sealed partial class EspnService(HttpClient httpClient, ILogger<EspnService> logger)
{
    private const int MaxAttempts = 3;

    // Soccer alone is ~20 leagues per day, so keep the burst against ESPN small.
    private static readonly SemaphoreSlim Throttle = new(8);

    private const string BaseUrl = "https://site.api.espn.com/apis/site/v2/sports/";
    private const string GolfUrl = BaseUrl + "golf/pga/scoreboard";

    private static readonly string[] TennisTours = ["atp", "wta"];
    private static readonly string[] TennisLateRounds = ["Quarterfinal", "Semifinal", "Final"];

    // Team sports: School uses the school name ("Liberty"), otherwise the full team name ("Boston Celtics").
    // groups=50 is every Division I game; without it ESPN returns only a featured handful.
    // PriorDay also reads the previous day's board for starts after midnight ET; soccer has none.
    private sealed record Scoreboard(
        string Path,
        string Sport,
        string League,
        bool School = false,
        string Query = "limit=300",
        bool PriorDay = true
    );

    private static readonly Scoreboard[] Scoreboards =
    [
        new("baseball/mlb", "Baseball", "MLB"),
        new("football/nfl", "Football", "NFL"),
        new("hockey/nhl", "Hockey", "NHL"),
        new("football/college-football", "Football", "NCAA", School: true),
        new("basketball/nba", "Basketball", "NBA"),
        new("basketball/wnba", "Basketball", "WNBA"),
        new("basketball/mens-college-basketball", "Basketball", "NCAA", School: true, Query: "groups=50&limit=400"),
        new("basketball/womens-college-basketball", "Basketball", "NCAA Women", School: true, Query: "groups=50&limit=400"),
        new("soccer/fifa.world", "Soccer", "FIFA World Cup", PriorDay: false),
        new("soccer/uefa.euro", "Soccer", "UEFA European Championship", PriorDay: false),
        new("soccer/conmebol.america", "Soccer", "Copa America", PriorDay: false),
        new("soccer/uefa.nations", "Soccer", "UEFA Nations League", PriorDay: false),
        new("soccer/fifa.friendly", "Soccer", "International Friendlies", PriorDay: false),
        new("soccer/uefa.champions", "Soccer", "UEFA Champions League", PriorDay: false),
        new("soccer/uefa.europa", "Soccer", "UEFA Europa League", PriorDay: false),
        new("soccer/uefa.europa.conf", "Soccer", "UEFA Conference League", PriorDay: false),
        new("soccer/eng.1", "Soccer", "Premier League", PriorDay: false),
        new("soccer/eng.2", "Soccer", "EFL Championship", PriorDay: false),
        new("soccer/eng.fa", "Soccer", "FA Cup", PriorDay: false),
        new("soccer/eng.league_cup", "Soccer", "EFL Cup", PriorDay: false),
        new("soccer/esp.1", "Soccer", "La Liga", PriorDay: false),
        new("soccer/ita.1", "Soccer", "Serie A", PriorDay: false),
        new("soccer/ger.1", "Soccer", "Bundesliga", PriorDay: false),
        new("soccer/fra.1", "Soccer", "Ligue 1", PriorDay: false),
        new("soccer/ned.1", "Soccer", "Eredivisie", PriorDay: false),
        new("soccer/por.1", "Soccer", "Primeira Liga", PriorDay: false),
        new("soccer/usa.1", "Soccer", "MLS", PriorDay: false),
        new("soccer/mex.1", "Soccer", "Liga MX", PriorDay: false),
        new("soccer/concacaf.leagues.cup", "Soccer", "Leagues Cup", PriorDay: false),
    ];

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
                LoadTennis(date, cancellationToken),
                LoadUfc(date, cancellationToken),
                .. Scoreboards.Select(board => LoadScoreboard(board, date, cancellationToken)),
            ]
        );
        return [.. feeds.SelectMany(events => events)];
    }

    // Null after retries; these feeds are optional, so the rest of the slate continues.
    private async Task<JsonDocument> Fetch(string url, string league, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            foreach (var (name, value) in BrowserHeaders)
            {
                request.Headers.TryAddWithoutValidation(name, value);
            }

            await Throttle.WaitAsync(cancellationToken);
            try
            {
                using var response = await httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                // A 4xx other than 429 won't improve on retry.
                var retryable = ex is not HttpRequestException { StatusCode: >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError and not HttpStatusCode.TooManyRequests };
                if (!retryable || attempt == MaxAttempts)
                {
                    logger.LogWarning(ex, "{league} feed request failed.", league);
                    return null;
                }
            }
            finally
            {
                Throttle.Release();
            }

            await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
        }
    }

    private static bool IsCanceled(JsonElement game) =>
        game.GetProperty("status").GetProperty("type").GetProperty("name").GetString()
            is "STATUS_CANCELED" or "STATUS_POSTPONED";

    // Team-sport games with the TV network ESPN lists.
    private async Task<List<SportsEvent>> LoadScoreboard(
        Scoreboard board,
        DateOnly date,
        CancellationToken cancellationToken
    )
    {
        // ESPN files late West Coast starts under the prior day, so read both and filter by ET date.
        // Date ranges return 400, so each day is its own request.
        var url = $"{BaseUrl}{board.Path}/scoreboard";
        DateOnly[] days = board.PriorDay ? [date.AddDays(-1), date] : [date];
        var documents = await Task.WhenAll(
            days.Select(day =>
                Fetch($"{url}?dates={day:yyyyMMdd}&{board.Query}", $"{board.Sport} {board.League}", cancellationToken)
            )
        );

        List<SportsEvent> events = [];
        foreach (var game in documents.Where(d => d is not null).SelectMany(d => d.RootElement.GetProperty("events").EnumerateArray()))
        {
            if (IsCanceled(game))
            {
                continue;
            }

            var competition = game.GetProperty("competitions")[0];
            var start = EasternTimeZone.Convert(competition.GetProperty("date").GetDateTimeOffset());
            if (EasternTimeZone.Date(start) != date)
            {
                continue;
            }

            string home = null;
            string away = null;
            foreach (var competitor in competition.GetProperty("competitors").EnumerateArray())
            {
                var team = competitor.GetProperty("team").GetProperty(board.School ? "location" : "displayName").GetString();
                if (competitor.GetProperty("homeAway").GetString() == "home")
                {
                    home = team;
                }
                else
                {
                    away = team;
                }
            }

            if (
                string.IsNullOrWhiteSpace(home)
                || string.IsNullOrWhiteSpace(away)
                || (board.Sport == "Soccer" && (YouthTeam().IsMatch(home) || YouthTeam().IsMatch(away)))
            )
            {
                continue;
            }

            // TBA start times carry a placeholder with timeValid=false.
            var timeValid = competition.TryGetProperty("timeValid", out var valid) && valid.GetBoolean();
            string[] networks = competition.TryGetProperty("broadcasts", out var broadcasts)
                ? [.. broadcasts.EnumerateArray().SelectMany(b => b.GetProperty("names").EnumerateArray()).Select(n => n.GetString())]
                : [];
            events.Add(
                new(board.Sport, board.League, home, away, timeValid ? start : null, url)
                {
                    Network = networks.Length > 0 ? string.Join(", ", networks) : null,
                }
            );
        }

        foreach (var document in documents)
        {
            document?.Dispose();
        }
        return events;
    }

    // One event per UFC card; ESPN's card date is the first bout, so the main-card time is left for the model to research.
    private async Task<List<SportsEvent>> LoadUfc(DateOnly date, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}mma/ufc/scoreboard";
        var documents = await Task.WhenAll(
            new[] { date.AddDays(-1), date }.Select(day => Fetch($"{url}?dates={day:yyyyMMdd}", "UFC", cancellationToken))
        );

        List<SportsEvent> events = [];
        foreach (var card in documents.Where(d => d is not null).SelectMany(d => d.RootElement.GetProperty("events").EnumerateArray()))
        {
            var name = card.GetProperty("name").GetString();
            if (
                !string.IsNullOrWhiteSpace(name)
                && !IsCanceled(card)
                && EasternTimeZone.Date(card.GetProperty("date").GetDateTimeOffset()) == date
            )
            {
                events.Add(new("MMA", "UFC", null, null, null, url) { EventName = name });
            }
        }

        foreach (var document in documents)
        {
            document?.Dispose();
        }
        return events;
    }

    [GeneratedRegex(@"\bU\d{2}$")]
    private static partial Regex YouthTeam();

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
            var start = EasternTimeZone.Date(tournament.GetProperty("date").GetDateTimeOffset());
            var end = EasternTimeZone.Date(tournament.GetProperty("endDate").GetDateTimeOffset());
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
                if (EasternTimeZone.Date(time) == date)
                {
                    startTime = time;
                }
            }

            events.Add(new("Golf", "PGA Tour", null, null, startTime, GolfUrl) { EventName = name });
        }
        return events;
    }

    // Grand Slams only: singles quarterfinals onward as their own matches, otherwise one entry per slam day.
    private async Task<List<SportsEvent>> LoadTennis(DateOnly date, CancellationToken cancellationToken)
    {
        var feeds = await Task.WhenAll(
            TennisTours.Select(async tour =>
            {
                var url = $"{BaseUrl}tennis/{tour}/scoreboard";
                return (Url: url, Document: await Fetch(url, $"Tennis {tour}", cancellationToken));
            })
        );

        // Slams are combined events, so ATP and WTA can both list the same tournament and matches.
        Dictionary<string, (string Url, DateTimeOffset? Start)> days = [];
        List<SportsEvent> matches = [];
        foreach (var (url, document) in feeds)
        {
            if (document is null)
            {
                continue;
            }

            using var _ = document;
            foreach (var tournament in document.RootElement.GetProperty("events").EnumerateArray())
            {
                var name = tournament.GetProperty("name").GetString();
                if (
                    string.IsNullOrWhiteSpace(name)
                    || !tournament.TryGetProperty("major", out var major)
                    || !major.GetBoolean()
                )
                {
                    continue;
                }

                var start = EasternTimeZone.Date(tournament.GetProperty("date").GetDateTimeOffset());
                var end = EasternTimeZone.Date(tournament.GetProperty("endDate").GetDateTimeOffset());
                // Overseas sessions can begin the prior ET evening, so widen a day.
                if (date < start.AddDays(-1) || date > end)
                {
                    continue;
                }

                days.TryAdd(name, (url, null));
                if (!tournament.TryGetProperty("groupings", out var groupings))
                {
                    continue;
                }

                foreach (var competition in groupings.EnumerateArray().SelectMany(g => g.GetProperty("competitions").EnumerateArray()))
                {
                    var type = competition.GetProperty("type");
                    if (
                        type.GetProperty("slug").GetString()?.EndsWith("singles", StringComparison.Ordinal) != true
                        || !competition.TryGetProperty("timeValid", out var timeValid)
                        || !timeValid.GetBoolean()
                    )
                    {
                        continue;
                    }

                    var time = EasternTimeZone.Convert(competition.GetProperty("date").GetDateTimeOffset());
                    if (EasternTimeZone.Date(time) != date)
                    {
                        continue;
                    }

                    if (days[name].Start is not { } earliest || time < earliest)
                    {
                        days[name] = (days[name].Url, time);
                    }

                    var round = competition.GetProperty("round").GetProperty("displayName").GetString();
                    string[] players =
                    [
                        .. competition.GetProperty("competitors").EnumerateArray()
                            .Select(c => c.GetProperty("athlete").GetProperty("displayName").GetString()),
                    ];
                    if (
                        TennisLateRounds.Contains(round)
                        && players.Length == 2
                        && players.All(p => !string.IsNullOrWhiteSpace(p) && p != "TBD")
                    )
                    {
                        matches.Add(
                            new("Tennis", name, null, null, time, url)
                            {
                                EventName = $"{players[0]} vs {players[1]} - {name} {type.GetProperty("text").GetString()} {round}",
                            }
                        );
                    }
                }
            }
        }

        return
        [
            .. matches.DistinctBy(m => m.EventName),
            .. days.Where(day => !matches.Any(m => m.League == day.Key))
                .Select(day => new SportsEvent("Tennis", day.Key, null, null, day.Value.Start, day.Value.Url) { EventName = day.Key }),
        ];
    }

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
                if (EasternTimeZone.Date(start) != date)
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
