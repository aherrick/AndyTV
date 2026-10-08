using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using AndyTV.Watchlist.Models;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

// FanDuel lines from Action Network's scoreboard API, trimmed to one row per game.
public sealed class ActionNetworkOddsService(HttpClient httpClient, ILogger<ActionNetworkOddsService> logger)
{
    private const string FanDuel = "69";

    private static readonly string[] Sports = ["nfl", "ncaaf", "nba", "ncaab", "mlb", "nhl", "wnba", "soccer"];

    public async Task<List<GameOdds>> GetOdds(DateOnly[] days, CancellationToken cancellationToken)
    {
        var feeds = await Task.WhenAll(
            days.SelectMany(day => Sports.Select(sport => Load(sport, day, cancellationToken)))
        );
        return [.. feeds.SelectMany(odds => odds)];
    }

    private async Task<List<GameOdds>> Load(string sport, DateOnly day, CancellationToken cancellationToken)
    {
        var date = day.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.actionnetwork.com/web/v2/scoreboard/{sport}?bookIds={FanDuel}&date={date}&periods=event"
        );
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0");

        JsonDocument document;
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            document =
                await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken)
                ?? throw new JsonException("Empty response.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            // Odds are optional; a failed sport just leaves its events without betting.
            logger.LogWarning(ex, "Action Network {sport} odds for {date} failed.", sport, date);
            return [];
        }

        using var _ = document;
        List<GameOdds> odds = [];
        if (!document.RootElement.TryGetProperty("games", out var games))
        {
            return odds;
        }

        foreach (var game in games.EnumerateArray())
        {
            if (
                !game.TryGetProperty("markets", out var markets)
                || !markets.TryGetProperty(FanDuel, out var book)
                || !book.TryGetProperty("event", out var lines)
            )
            {
                continue;
            }

            // NFL/NCAAF return the whole week for any date in it.
            var start = EasternTimeZone.Convert(game.GetProperty("start_time").GetDateTimeOffset());
            if (EasternTimeZone.Date(start) != day)
            {
                continue;
            }

            var awayId = game.GetProperty("away_team_id").GetInt32();
            var homeId = game.GetProperty("home_team_id").GetInt32();
            string away = null;
            string home = null;
            foreach (var team in game.GetProperty("teams").EnumerateArray())
            {
                var id = team.GetProperty("id").GetInt32();
                if (id == awayId)
                {
                    away = team.GetProperty("full_name").GetString();
                }
                else if (id == homeId)
                {
                    home = team.GetProperty("full_name").GetString();
                }
            }

            var row = new GameOdds(
                sport,
                away,
                home,
                start,
                Line(lines, "spread", "away", "value"),
                Line(lines, "spread", "home", "value"),
                Line(lines, "moneyline", "away", "odds"),
                Line(lines, "moneyline", "home", "odds"),
                Line(lines, "total", "over", "value")
            );
            if (row is { AwaySpread: null, AwayMoneyline: null, HomeMoneyline: null, Total: null })
            {
                continue;
            }

            odds.Add(row);
        }
        return odds;
    }

    private static decimal? Line(JsonElement lines, string market, string side, string field)
    {
        if (!lines.TryGetProperty(market, out var outcomes))
        {
            return null;
        }

        foreach (var outcome in outcomes.EnumerateArray())
        {
            if (
                outcome.GetProperty("side").GetString() == side
                && outcome.TryGetProperty(field, out var value)
                && value.ValueKind == JsonValueKind.Number
            )
            {
                return value.GetDecimal();
            }
        }
        return null;
    }
}
