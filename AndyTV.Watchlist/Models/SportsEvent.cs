namespace AndyTV.Watchlist.Models;

// One schedule candidate from the ESPN feeds. Single-name events (racing, golf, tennis, UFC) use EventName
// with null team fields; a null StartTimeIso means the model researches the time.
public sealed record SportsEvent(
    string Sport,
    string League,
    string HomeTeam,
    string AwayTeam,
    DateTimeOffset? StartTimeIso,
    string SourceUrl
)
{
    public string EventName { get; init; }

    public string Network { get; init; }

    public Betting Betting { get; init; }

    public string Matchup => EventName ?? $"{AwayTeam} @ {HomeTeam}";
}
