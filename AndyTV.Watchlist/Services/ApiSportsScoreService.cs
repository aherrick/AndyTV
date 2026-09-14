using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using Raffinert.FuzzySharp;

namespace AndyTV.Watchlist.Services;

public sealed class ApiSportsScoreService(HttpClient http, AppSettings settings)
{
    private static readonly Dictionary<string, string> SportHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["soccer"] = "https://v3.football.api-sports.io",
        ["football"] = "https://v1.american-football.api-sports.io",
        ["basketball"] = "https://v1.basketball.api-sports.io",
        ["baseball"] = "https://v1.baseball.api-sports.io",
        ["hockey"] = "https://v1.hockey.api-sports.io",
    };

    private int keyIndex = -1;

    public async Task EnrichAsync(IEnumerable<WatchlistGame> games, CancellationToken cancellationToken = default)
    {
        var routed = games.Select(game => (Game: game, Sport: GetSport(game))).ToList();
        foreach (var item in routed)
        {
            item.Game.Score = null;
        }

        var responses = routed
            .Select(item => item.Sport)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(sport => sport, sport => GetLiveGames(sport, cancellationToken), StringComparer.OrdinalIgnoreCase);

        await Task.WhenAll(responses.Values);

        foreach (var (game, sport) in routed)
        {
            var match = FindGame(game, responses[sport].Result);
            if (match is not null)
            {
                game.Score = new(match.AwayScore, match.HomeScore, "in", match.Detail, DateTimeOffset.UtcNow);
            }
        }
    }

    private async Task<List<LiveGame>> GetLiveGames(string sport, CancellationToken cancellationToken)
    {
        if (!SportHosts.TryGetValue(sport, out var host))
        {
            return [];
        }

        var date = EasternTimeZone.Now.ToString("yyyy-MM-dd");
        var path = sport == "soccer" ? "fixtures" : "games";
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{host}/{path}?date={date}");
        var key = GetNextKey();
        if (key is null)
        {
            return [];
        }

        request.Headers.Add("x-apisports-key", key);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return ParseGames(document.RootElement, sport);
    }

    private string? GetNextKey()
    {
        var keys = new[] { settings.ApiSportsKey1, settings.ApiSportsKey2 }
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToArray();
        return keys.Length == 0 ? null : keys[Interlocked.Increment(ref keyIndex) % keys.Length];
    }

    private static string GetSport(WatchlistGame game)
    {
        var league = game.League.Trim().ToUpperInvariant();
        if (league is "NFL" or "NCAAF" or "CFB")
        {
            return "football";
        }

        if (league is "NBA" or "WNBA" or "NCAAM" or "NCAAW")
        {
            return "basketball";
        }

        if (league == "MLB")
        {
            return "baseball";
        }

        if (league == "NHL")
        {
            return "hockey";
        }

        return "soccer";
    }

    private static List<LiveGame> ParseGames(JsonElement root, string sport)
    {
        var games = new List<LiveGame>();
        if (!root.TryGetProperty("response", out var response) || response.ValueKind != JsonValueKind.Array)
        {
            return games;
        }

        foreach (var item in response.EnumerateArray())
        {
            var teams = item.TryGetProperty("teams", out var t) ? t : default;
            var away = ReadString(teams, "away", "name");
            var home = ReadString(teams, "home", "name");
            var status = item.TryGetProperty("status", out var s) ? s : default;
            var shortStatus = ReadString(status, "short");
            var longStatus = ReadString(status, "long");
            if (!IsLive(shortStatus, longStatus) || away is null || home is null)
            {
                continue;
            }

            var scores = item.TryGetProperty("scores", out var scoreObject) ? scoreObject : default;
            var goals = item.TryGetProperty("goals", out var goalObject) ? goalObject : default;
            var awayScore = ReadScore(scores, goals, "away", sport);
            var homeScore = ReadScore(scores, goals, "home", sport);
            var detail = ReadString(status, "elapsed") is { } elapsed ? $"{elapsed}'" : longStatus ?? shortStatus;
            games.Add(new LiveGame(away, home, awayScore, homeScore, detail));
        }

        return games;
    }

    private static bool IsLive(string? shortStatus, string? longStatus) =>
        shortStatus is not (null or "FT" or "AOT" or "PEN" or "NS" or "TBD" or "CANC" or "PST" or "ABD")
        && !string.Equals(longStatus, "Not Started", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(longStatus, "Finished", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(longStatus, "Final", StringComparison.OrdinalIgnoreCase);

    private static string? ReadScore(JsonElement scores, JsonElement goals, string side, string sport)
    {
        var property = sport is "soccer" or "hockey" ? goals : scores;
        var valueName = sport == "baseball" ? "runs" : "total";
        if (property.ValueKind == JsonValueKind.Object
            && property.TryGetProperty(side, out var sideValue)
            && sideValue.TryGetProperty(valueName, out var value))
        {
            return value.ValueKind == JsonValueKind.Null ? "0" : value.ToString();
        }

        return "0";
    }

    private static string? ReadString(JsonElement element, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out element))
            {
                return null;
            }
        }

        return element.ValueKind is JsonValueKind.String or JsonValueKind.Number ? element.ToString() : null;
    }

    private static LiveGame? FindGame(WatchlistGame game, IEnumerable<LiveGame> liveGames) =>
        liveGames
            .Select(candidate => new
            {
                Game = candidate,
                Score = Math.Max(
                    TeamScore(game.AwayTeam, candidate.AwayTeam) + TeamScore(game.HomeTeam, candidate.HomeTeam),
                    TeamScore(game.AwayTeam, candidate.HomeTeam) + TeamScore(game.HomeTeam, candidate.AwayTeam)),
            })
            .Where(candidate => candidate.Score >= 160)
            .OrderByDescending(candidate => candidate.Score)
            .Select(candidate => candidate.Game)
            .FirstOrDefault();

    private static double TeamScore(string? name, string candidate) =>
        name is null ? 0 : Fuzz.WeightedRatio(name.ToLowerInvariant(), candidate.ToLowerInvariant());

    private sealed record LiveGame(string AwayTeam, string HomeTeam, string? AwayScore, string? HomeScore, string? Detail);
}
