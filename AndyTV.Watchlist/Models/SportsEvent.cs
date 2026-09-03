namespace AndyTV.Watchlist.Models;

public sealed record SportsEvent(
    string Sport,
    string League,
    string Home,
    string Away,
    DateTimeOffset StartTimeEastern
)
{
    public string Matchup => $"{Away} @ {Home}";
}
