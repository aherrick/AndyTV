using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

public static class SportsEventExtensions
{
    // A game's ESPN event: its matchup (the prompt has the model copy matchups exactly) on its day, with the nearest
    // start deciding a doubleheader. Matching the day too means a feed that lacks the game (Saturday's, for a weekend
    // series' Sunday game) never hands it another day's event. Null when none matches or the game has no start.
    public static SportsEvent FindGame(this IEnumerable<SportsEvent> events, string matchup, DateTimeOffset? start)
    {
        if (start is not { } time)
        {
            return null;
        }

        var day = EasternTimeZone.Date(time);
        // An event without a start (TBA) takes the game's.
        return events
            .Where(e => e.Matchup == matchup && EasternTimeZone.Date(e.StartTimeIso ?? time) == day)
            .MinBy(e => ((e.StartTimeIso ?? time) - time).Duration());
    }
}
