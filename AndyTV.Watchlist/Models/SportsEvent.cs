namespace AndyTV.Watchlist.Models;

// One schedule candidate from the sports feeds. Team fields are null for racing/MMA,
// and StartTimeIso is null for UFC cards (the model researches the main-card time).
public sealed record SportsEvent(
    string Sport,
    string League,
    string? HomeTeam,
    string? AwayTeam,
    DateTimeOffset? StartTimeIso,
    string SourceUrl
)
{
    public string? EventName { get; init; }

    public string Matchup => EventName ?? $"{AwayTeam} @ {HomeTeam}";
}
