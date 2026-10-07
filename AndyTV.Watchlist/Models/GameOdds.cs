namespace AndyTV.Watchlist.Models;

// One FanDuel line row from Action Network, supplied to the model for matching.
public sealed record GameOdds(
    string Sport,
    string AwayTeam,
    string HomeTeam,
    DateTimeOffset StartTimeIso,
    decimal? AwaySpread,
    decimal? HomeSpread,
    decimal? AwayMoneyline,
    decimal? HomeMoneyline,
    decimal? Total
);
