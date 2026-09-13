namespace AndyTV.Watchlist.Models;

// Refit uses System.Text.Json's web defaults; only model the fields we consume.
public sealed class EspnScoreboard
{
    public List<EspnEvent> Events { get; init; } = [];
}

public sealed class EspnEvent
{
    public DateTimeOffset Date { get; init; }
    public EspnStatus Status { get; init; } = new();
    public List<EspnCompetition> Competitions { get; init; } = [];
    public EspnCompetitor? Home => Competitions.FirstOrDefault()?.Competitors.FirstOrDefault(x => x.HomeAway == "home");
    public EspnCompetitor? Away => Competitions.FirstOrDefault()?.Competitors.FirstOrDefault(x => x.HomeAway == "away");
}

public sealed class EspnCompetition
{
    public List<EspnCompetitor> Competitors { get; init; } = [];
}

public sealed class EspnCompetitor
{
    public string HomeAway { get; init; } = "";
    public string? Score { get; init; }
    public EspnTeam Team { get; init; } = new();
}

public sealed class EspnTeam
{
    public string DisplayName { get; init; } = "";
    public string ShortDisplayName { get; init; } = "";
    public string Abbreviation { get; init; } = "";
}

public sealed class EspnStatus
{
    public EspnStatusType Type { get; init; } = new();
}

public sealed class EspnStatusType
{
    public string State { get; init; } = "";
    public string? ShortDetail { get; init; }
}
