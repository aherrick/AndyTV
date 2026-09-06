using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public static class SportsFormat
{
    public static string Icon(string sport) =>
        sport switch
        {
            "Baseball" => "⚾",
            "Football" => "🏈",
            "Hockey" => "🏒",
            "Basketball" => "🏀",
            "Soccer" => "⚽",
            "Racing" => "🏁",
            _ => "📺",
        };

    public static string RankedMatchup(SportsEvent sportsEvent, RankedEvent? rankedEvent)
    {
        if (string.IsNullOrEmpty(sportsEvent.Away))
        {
            return sportsEvent.Home;
        }

        var away = rankedEvent?.AwayRank is int ar ? $"#{ar} {sportsEvent.Away}" : sportsEvent.Away;
        var home = rankedEvent?.HomeRank is int hr ? $"#{hr} {sportsEvent.Home}" : sportsEvent.Home;
        return $"{away} @ {home}";
    }
}
