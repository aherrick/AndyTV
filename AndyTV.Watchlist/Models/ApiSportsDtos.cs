using System.Text.Json;
using System.Text.Json.Serialization;

namespace AndyTV.Watchlist.Models;

internal sealed class ApiSportsResponse<T>
{
    [JsonPropertyName("response")]
    public List<T> Response { get; init; } = [];

    [JsonPropertyName("errors")]
    public JsonElement Errors { get; init; }
}

internal sealed class LeagueDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
}

internal sealed class TeamDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
}

internal sealed class TeamsDto
{
    [JsonPropertyName("home")]
    public TeamDto Home { get; init; } = new();

    [JsonPropertyName("away")]
    public TeamDto Away { get; init; } = new();
}

internal sealed class BaseballGameDto
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("league")]
    public LeagueDto League { get; init; } = new();

    [JsonPropertyName("teams")]
    public TeamsDto Teams { get; init; } = new();
}

internal sealed class FootballGameDto
{
    [JsonPropertyName("game")]
    public FootballGameDetailsDto Game { get; init; } = new();

    [JsonPropertyName("league")]
    public LeagueDto League { get; init; } = new();

    [JsonPropertyName("teams")]
    public TeamsDto Teams { get; init; } = new();
}

internal sealed class FootballGameDetailsDto
{
    [JsonPropertyName("date")]
    public FootballGameDateDto Date { get; init; } = new();
}

internal sealed class FootballGameDateDto
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }
}

internal sealed class HockeyGameDto
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("league")]
    public LeagueDto League { get; init; } = new();

    [JsonPropertyName("teams")]
    public TeamsDto Teams { get; init; } = new();
}

internal sealed class SoccerFixtureDto
{
    [JsonPropertyName("fixture")]
    public SoccerFixtureDetailsDto Fixture { get; init; } = new();

    [JsonPropertyName("league")]
    public LeagueDto League { get; init; } = new();

    [JsonPropertyName("teams")]
    public TeamsDto Teams { get; init; } = new();
}

internal sealed class SoccerFixtureDetailsDto
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }
}
