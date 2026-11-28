namespace AndyTV.Watchlist.Models;

// One schedule candidate from the sports feeds. Single-name events (racing, golf, tennis, UFC) use EventName
// with null team fields; a null StartTimeIso means the model researches the time.
public sealed record SportsEvent(
    string Sport,
    string League,
    string HomeTeam,
    string AwayTeam,
    DateTimeOffset? StartTimeIso
)
{
    public string EventName { get; init; }

    public string Network { get; init; }

    public Betting Betting { get; init; }

    // ESPN's team abbreviations (e.g. "BAL"), so the odds can name the spread favorite; null outside ESPN team boards.
    // Soccer uses ESPN's short names instead ("Man Utd", not "MAN"), since its codes mean little to US viewers.
    public string AwayAbbr { get; init; }
    public string HomeAbbr { get; init; }

    // Round and series state, e.g. "ALDS - Game 5 · Series tied 2-2".
    public string Note { get; init; }

    public bool NeutralSite { get; init; }

    public string Matchup => EventName ?? $"{AwayTeam} {(NeutralSite ? "vs" : "@")} {HomeTeam}";

    // This event with a fresher feed's odds and abbreviations for it.
    public SportsEvent WithOddsFrom(SportsEvent fresh) =>
        this with { Betting = fresh.Betting, AwayAbbr = fresh.AwayAbbr, HomeAbbr = fresh.HomeAbbr };
}
