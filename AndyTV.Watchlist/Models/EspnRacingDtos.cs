using System.Text.Json.Serialization;

namespace AndyTV.Watchlist.Models;

internal sealed class EspnScoreboardDto
{
    [JsonPropertyName("events")]
    public List<EspnRaceDto> Events { get; init; } = [];
}

internal sealed class EspnRaceDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("shortName")]
    public string ShortName { get; init; } = "";

    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; init; }

    [JsonPropertyName("competitions")]
    public List<EspnCompetitionDto> Competitions { get; init; } = [];
}

internal sealed class EspnCompetitionDto
{
    [JsonPropertyName("date")]
    public DateTimeOffset Date { get; init; }

    [JsonPropertyName("type")]
    public EspnCompetitionTypeDto Type { get; init; } = new();
}

internal sealed class EspnCompetitionTypeDto
{
    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; init; } = "";
}
