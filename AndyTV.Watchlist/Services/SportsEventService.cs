using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

// Every sport in the watchlist, from ESPN's public scoreboards (no key), plus CFL from TheSportsDB.
public sealed class SportsEventService(HttpClient httpClient, ILogger<SportsEventService> logger)
{
    private const int MaxAttempts = 3;

    // Soccer alone is ~30 boards per day, so keep the burst against ESPN small.
    private static readonly SemaphoreSlim Throttle = new(8);

    private const string BaseUrl = "https://site.api.espn.com/apis/site/v2/sports/";

    // ESPN's CFL data stops at 2023, so CFL comes from TheSportsDB's free tier (league 4405).
    private const string CflUrl = "https://www.thesportsdb.com/api/v1/json/123/eventsday.php?l=4405&d=";

    private static readonly string[] TennisTours = ["atp", "wta"];
    private static readonly string[] TennisLateRounds = ["Quarterfinal", "Semifinal", "Final"];

    // Team sports: School uses the school name ("Liberty"), otherwise the full team name ("Boston Celtics").
    // groups=50 is every Division I game; without it ESPN returns only a featured handful.
    private sealed record Scoreboard(
        string Path,
        string Sport,
        string League,
        bool School = false,
        string Query = "limit=300"
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
        new("soccer/fifa.world", "Soccer", "FIFA World Cup"),
        new("soccer/fifa.wwc", "Soccer", "FIFA Women's World Cup"),
        new("soccer/fifa.cwc", "Soccer", "FIFA Club World Cup"),
        new("soccer/fifa.worldq.uefa", "Soccer", "World Cup Qualifying - UEFA"),
        new("soccer/fifa.worldq.conmebol", "Soccer", "World Cup Qualifying - CONMEBOL"),
        new("soccer/fifa.worldq.concacaf", "Soccer", "World Cup Qualifying - Concacaf"),
        new("soccer/uefa.euro", "Soccer", "UEFA European Championship"),
        new("soccer/conmebol.america", "Soccer", "Copa America"),
        new("soccer/concacaf.gold", "Soccer", "Concacaf Gold Cup"),
        new("soccer/uefa.nations", "Soccer", "UEFA Nations League"),
        new("soccer/fifa.friendly", "Soccer", "International Friendlies"),
        new("soccer/fifa.friendly.w", "Soccer", "Women's International Friendlies"),
        new("soccer/uefa.champions", "Soccer", "UEFA Champions League"),
        new("soccer/uefa.europa", "Soccer", "UEFA Europa League"),
        new("soccer/uefa.europa.conf", "Soccer", "UEFA Conference League"),
        new("soccer/conmebol.libertadores", "Soccer", "Copa Libertadores"),
        new("soccer/concacaf.champions", "Soccer", "Concacaf Champions Cup"),
        new("soccer/eng.1", "Soccer", "Premier League"),
        new("soccer/eng.2", "Soccer", "EFL Championship"),
        new("soccer/eng.fa", "Soccer", "FA Cup"),
        new("soccer/eng.league_cup", "Soccer", "EFL Cup"),
        new("soccer/esp.1", "Soccer", "La Liga"),
        new("soccer/ita.1", "Soccer", "Serie A"),
        new("soccer/ger.1", "Soccer", "Bundesliga"),
        new("soccer/fra.1", "Soccer", "Ligue 1"),
        new("soccer/ned.1", "Soccer", "Eredivisie"),
        new("soccer/por.1", "Soccer", "Primeira Liga"),
        new("soccer/usa.1", "Soccer", "MLS"),
        new("soccer/usa.nwsl", "Soccer", "NWSL"),
        new("soccer/mex.1", "Soccer", "Liga MX"),
        new("soccer/concacaf.leagues.cup", "Soccer", "Leagues Cup"),
    ];

    // ESPN scoreboard slugs and the session types worth watching for each series (F1: SS = sprint qualifying, SR = sprint).
    private static readonly (string Slug, string League, string[] Sessions)[] Series =
    [
        ("nascar-premier", "NASCAR Cup Series", ["Race"]),
        ("irl", "IndyCar Series", ["Race"]),
        ("f1", "Formula 1", ["Race", "Qual", "SR", "SS"]),
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
            [
                .. Series.Select(series =>
                    LoadRacing(series.Slug, series.League, series.Sessions, date, cancellationToken)
                ),
                LoadGolf(date, cancellationToken),
                LoadTennis(date, cancellationToken),
                LoadUfc(date, cancellationToken),
                LoadCfl(date, cancellationToken),
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

    // The feed's events, cloned so the document can be released; a failed feed or "events": null (TheSportsDB) adds nothing.
    private async Task<List<JsonElement>> FetchEvents(string url, string league, CancellationToken cancellationToken)
    {
        using var document = await Fetch(url, league, cancellationToken);
        return document?.RootElement.GetProperty("events") is { ValueKind: JsonValueKind.Array } events
            ? [.. events.EnumerateArray().Select(e => e.Clone())]
            : [];
    }

    private static bool IsCanceled(JsonElement game) =>
        game.GetProperty("status").GetProperty("type").GetProperty("name").GetString()
            is "STATUS_CANCELED" or "STATUS_POSTPONED";

    // The value at a property path, e.g. At(odds, "moneyline", "home", "close", "odds"); null when a step is missing or not an object.
    private static JsonElement? At(JsonElement element, params ReadOnlySpan<string> path)
    {
        foreach (var name in path)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var next))
            {
                return null;
            }
            element = next;
        }
        return element;
    }

    // Optional flags like "timeValid"; missing counts as false.
    private static bool IsTrue(JsonElement element, string name) => At(element, name)?.ValueKind == JsonValueKind.True;

    // ESPN's "date" (an event's or session's start) in Eastern time.
    private static DateTimeOffset Start(JsonElement element) =>
        EasternTimeZone.Convert(element.GetProperty("date").GetDateTimeOffset());

    // Golf and tennis tournaments run date..endDate; overseas play can begin the prior ET evening, so widen a day.
    private static bool InWindow(JsonElement tournament, DateOnly date) =>
        date >= EasternTimeZone.Date(Start(tournament)).AddDays(-1)
        && date <= EasternTimeZone.Date(tournament.GetProperty("endDate").GetDateTimeOffset());

    // Team-sport games with the TV network and odds ESPN lists.
    private async Task<List<SportsEvent>> LoadScoreboard(
        Scoreboard board,
        DateOnly date,
        CancellationToken cancellationToken
    )
    {
        // ESPN files each game under its sports day, so this one board holds the whole date, including
        // late West Coast and Hawaii games that tip after midnight ET (see the date check below).
        var games = await FetchEvents(
            $"{BaseUrl}{board.Path}/scoreboard?dates={date:yyyyMMdd}&{board.Query}",
            $"{board.Sport} {board.League}",
            cancellationToken
        );

        List<SportsEvent> events = [];
        foreach (var game in games)
        {
            if (IsCanceled(game))
            {
                continue;
            }

            var competition = game.GetProperty("competitions")[0];
            var start = Start(competition);
            // A game that tips after midnight ET is still on this date's board: ESPN lists it under the
            // day it belongs to, not under the next calendar day (e.g. a 9:30 PM Las Vegas tip is 12:30 AM ET
            // the next day, yet it's on this board and not on the next day's). Its ET date is the next day,
            // but we don't cut it for that. It's already in this response, so no extra request is needed,
            // and the next day's board doesn't list it, so it isn't counted twice. Only games before 4 AM ET
            // are kept this way, which covers the latest Hawaii tips.
            var day = EasternTimeZone.Date(start);
            if (day != date && !(day == date.AddDays(1) && start.Hour < 4))
            {
                continue;
            }

            string home = null;
            string away = null;
            string homeAbbr = null;
            string awayAbbr = null;
            foreach (var competitor in competition.GetProperty("competitors").EnumerateArray())
            {
                var team = competitor.GetProperty("team").GetProperty(board.School ? "location" : "displayName").GetString();
                // ESPN's curated rank is the AP Top 25 (99 = unranked).
                if (board.School && At(competitor, "curatedRank", "current")?.GetInt32() is >= 1 and <= 25 and var position)
                {
                    team = $"#{position} {team}";
                }
                // Soccer's codes ("M05", "MAN") mean little to US viewers, so it names the team ("Mainz", "Man Utd").
                var abbr = At(competitor, "team", board.Sport == "Soccer" ? "shortDisplayName" : "abbreviation")?.GetString();
                if (competitor.GetProperty("homeAway").GetString() == "home")
                {
                    (home, homeAbbr) = (team, abbr);
                }
                else
                {
                    (away, awayAbbr) = (team, abbr);
                }
            }

            if (string.IsNullOrWhiteSpace(home) || string.IsNullOrWhiteSpace(away))
            {
                continue;
            }

            // TBA start times carry a placeholder with timeValid=false.
            var timeValid = IsTrue(competition, "timeValid");
            string[] networks = competition.TryGetProperty("broadcasts", out var broadcasts)
                ? [.. broadcasts.EnumerateArray().SelectMany(b => b.GetProperty("names").EnumerateArray()).Select(n => n.GetString())]
                : [];
            var neutralSite = IsTrue(competition, "neutralSite");
            events.Add(
                new(board.Sport, board.League, home, away, timeValid ? start : null)
                {
                    Network = networks.Length > 0 ? string.Join(", ", networks) : null,
                    Betting = Odds(competition),
                    AwayAbbr = awayAbbr,
                    HomeAbbr = homeAbbr,
                    Note = Note(competition, neutralSite),
                    NeutralSite = neutralSite,
                }
            );
        }
        return events;
    }

    // ESPN's round headline, series summary, and (neutral sites only) venue city, e.g. "ALDS - Game 5 · Series tied 2-2"; null when none exist.
    private static string Note(JsonElement competition, bool neutralSite)
    {
        List<string> parts = [];
        if (At(competition, "notes") is { ValueKind: JsonValueKind.Array } notes)
        {
            parts.AddRange(notes.EnumerateArray().Select(n => At(n, "headline")?.GetString()));
        }
        parts.Add(At(competition, "series", "summary")?.GetString());
        if (neutralSite && At(competition, "venue", "address") is { } address && At(address, "city") is { } city)
        {
            // International venues have no state; fall back to the country.
            var region = (At(address, "state") ?? At(address, "country"))?.GetString();
            parts.Add(string.IsNullOrWhiteSpace(region) ? city.GetString() : $"{city.GetString()}, {region}");
        }
        parts.RemoveAll(string.IsNullOrWhiteSpace);
        return parts.Count > 0 ? string.Join(" · ", parts) : null;
    }

    // ESPN's sportsbook lines for the game; null when none are posted. DraftKings lines come as American odds per
    // market; bet365 (some soccer) sends only decimal win/draw/win prices, so those fill in the moneylines.
    private static Betting Odds(JsonElement competition)
    {
        // Smaller boards (e.g. women's friendlies) send "odds": [null] when no lines are posted.
        if (
            !competition.TryGetProperty("odds", out var all)
            || all.ValueKind != JsonValueKind.Array
            || all.GetArrayLength() == 0
            || all[0].ValueKind != JsonValueKind.Object
        )
        {
            return null;
        }

        var odds = all[0];
        var betting = new Betting(
            Line(odds, "pointSpread", "away", "line"),
            Line(odds, "pointSpread", "home", "line"),
            Line(odds, "moneyline", "away", "odds") ?? LineFromDecimal(odds, "awayTeamOdds"),
            Line(odds, "moneyline", "home", "odds") ?? LineFromDecimal(odds, "homeTeamOdds"),
            odds.TryGetProperty("overUnder", out var total) && total.ValueKind == JsonValueKind.Number ? total.GetDecimal() : null,
            Line(odds, "moneyline", "draw", "odds") ?? LineFromDecimal(odds, "drawOdds")
        );
        return betting is { AwaySpread: null, HomeSpread: null, AwayMoneyline: null, HomeMoneyline: null, Total: null, DrawMoneyline: null }
            ? null
            : betting;
    }

    // Lines arrive as strings like "+7.5" or "-305"; "EVEN" is +100 and anything else ("OFF") is missing.
    private static decimal? Line(JsonElement odds, string market, string side, string field)
    {
        if (At(odds, market, side, "close", field) is not { ValueKind: JsonValueKind.String } value)
        {
            return null;
        }

        var text = value.GetString();
        if (text == "EVEN")
        {
            return 100;
        }
        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;
    }

    // A bet365 decimal price (e.g. "value": 8.0) as an American line (+700); null when missing. 2.0 is even money:
    // longer prices win (price - 1) x 100 per 100 staked, shorter ones need 100 / (price - 1) to win 100.
    private static decimal? LineFromDecimal(JsonElement odds, string side)
    {
        var price = At(odds, side, "value") is { ValueKind: JsonValueKind.Number } value ? value.GetDecimal() : 0;
        if (price <= 1)
        {
            return null;
        }
        return Math.Round(price >= 2 ? (price - 1) * 100 : -100 / (price - 1), MidpointRounding.AwayFromZero);
    }

    // TheSportsDB buckets games by UTC day, so late western kickoffs land on the next day's list.
    private async Task<List<SportsEvent>> LoadCfl(DateOnly date, CancellationToken cancellationToken)
    {
        DateOnly[] days = [date, date.AddDays(1)];
        var feeds = await Task.WhenAll(
            days.Select(day => FetchEvents($"{CflUrl}{day:yyyy-MM-dd}", "CFL", cancellationToken))
        );

        List<SportsEvent> events = [];
        foreach (var game in feeds.SelectMany(games => games))
        {
            var home = game.GetProperty("strHomeTeam").GetString();
            var away = game.GetProperty("strAwayTeam").GetString();
            if (
                string.IsNullOrWhiteSpace(home)
                || string.IsNullOrWhiteSpace(away)
                || game.GetProperty("strPostponed").GetString() == "yes"
                || !DateTime.TryParse(
                    game.GetProperty("strTimestamp").GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var utc
                )
            )
            {
                continue;
            }

            var start = EasternTimeZone.Convert(new DateTimeOffset(utc, TimeSpan.Zero));
            if (EasternTimeZone.Date(start) == date)
            {
                events.Add(new("Football", "CFL", home, away, start));
            }
        }
        return events;
    }

    // One event per UFC card; ESPN's card date is the first bout, so the main-card time is left for the model to research.
    private async Task<List<SportsEvent>> LoadUfc(DateOnly date, CancellationToken cancellationToken)
    {
        var cards = await FetchEvents($"{BaseUrl}mma/ufc/scoreboard?dates={date:yyyyMMdd}", "UFC", cancellationToken);

        return
        [
            .. cards
                .Where(card => !IsCanceled(card) && EasternTimeZone.Date(Start(card)) == date)
                .Select(card => card.GetProperty("name").GetString())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => new SportsEvent("MMA", "UFC", null, null, null) { EventName = name }),
        ];
    }

    private async Task<List<SportsEvent>> LoadGolf(DateOnly date, CancellationToken cancellationToken)
    {
        List<SportsEvent> events = [];
        foreach (var tournament in await FetchEvents($"{BaseUrl}golf/pga/scoreboard", "PGA Tour", cancellationToken))
        {
            var name = tournament.GetProperty("name").GetString();
            if (string.IsNullOrWhiteSpace(name) || !InWindow(tournament, date) || IsCanceled(tournament))
            {
                continue;
            }

            // ESPN's date is a midnight placeholder unless timeValid is true; null lets the model research it.
            var competition = tournament.GetProperty("competitions")[0];
            DateTimeOffset? startTime = null;
            if (IsTrue(competition, "timeValid"))
            {
                var time = Start(competition);
                if (EasternTimeZone.Date(time) == date)
                {
                    startTime = time;
                }
            }

            events.Add(new("Golf", "PGA Tour", null, null, startTime) { EventName = name });
        }
        return events;
    }

    // Grand Slams only: singles quarterfinals onward as their own matches, otherwise one entry per slam day.
    private async Task<List<SportsEvent>> LoadTennis(DateOnly date, CancellationToken cancellationToken)
    {
        var feeds = await Task.WhenAll(
            TennisTours.Select(tour => FetchEvents($"{BaseUrl}tennis/{tour}/scoreboard", $"Tennis {tour}", cancellationToken))
        );

        // Slams are combined events, so ATP and WTA can both list the same tournament and matches.
        Dictionary<string, DateTimeOffset?> days = [];
        List<SportsEvent> matches = [];
        foreach (var tournament in feeds.SelectMany(tournaments => tournaments))
        {
            var name = tournament.GetProperty("name").GetString();
            if (string.IsNullOrWhiteSpace(name) || !IsTrue(tournament, "major") || !InWindow(tournament, date))
            {
                continue;
            }

            days.TryAdd(name, null);
            if (!tournament.TryGetProperty("groupings", out var groupings))
            {
                continue;
            }

            foreach (var competition in groupings.EnumerateArray().SelectMany(g => g.GetProperty("competitions").EnumerateArray()))
            {
                var type = competition.GetProperty("type");
                if (
                    type.GetProperty("slug").GetString()?.EndsWith("singles", StringComparison.Ordinal) != true
                    || !IsTrue(competition, "timeValid")
                )
                {
                    continue;
                }

                var time = Start(competition);
                if (EasternTimeZone.Date(time) != date)
                {
                    continue;
                }

                if (days[name] is not { } earliest || time < earliest)
                {
                    days[name] = time;
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
                        new("Tennis", name, null, null, time)
                        {
                            EventName = $"{players[0]} vs {players[1]} - {name} {type.GetProperty("text").GetString()} {round}",
                        }
                    );
                }
            }
        }

        return
        [
            .. matches.DistinctBy(m => m.EventName),
            .. days.Where(day => !matches.Any(m => m.League == day.Key))
                .Select(day => new SportsEvent("Tennis", day.Key, null, null, day.Value) { EventName = day.Key }),
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
        List<SportsEvent> events = [];
        foreach (var race in await FetchEvents($"{BaseUrl}racing/{slug}/scoreboard", league, cancellationToken))
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
                var session = At(competition, "type", "abbreviation") is { } abbreviation
                    ? abbreviation.GetString()
                    : competitions.GetArrayLength() == 1 ? "Race" : null;
                if (session is null || !sessions.Contains(session, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var start = Start(competition);
                if (EasternTimeZone.Date(start) != date)
                {
                    continue;
                }

                var suffix = session.ToLowerInvariant() switch
                {
                    "qual" => " - Qualifying",
                    "sr" => " - Sprint",
                    "ss" => " - Sprint Qualifying",
                    _ => "",
                };
                events.Add(new("Racing", league, null, null, start) { EventName = name + suffix });
            }
        }
        return events;
    }
}
