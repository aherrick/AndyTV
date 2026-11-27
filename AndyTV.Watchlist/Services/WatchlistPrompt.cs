using System.Globalization;
using System.Text.Json;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

// The WATCHLIST_PROMPT app setting; {dates}, {firstDay} and {count} are filled in and the supplied events appended.
// Events go without their odds: the model doesn't rank by them, and code attaches them to its picks.
public static class WatchlistPrompt
{
    public static string Build(string template, DateOnly[] days, List<SportsEvent> events)
    {
        var eventsJson = JsonSerializer.Serialize(
            events.Select(sportsEvent => new
            {
                sportsEvent.Sport,
                sportsEvent.League,
                sportsEvent.Matchup,
                StartTimeIso = sportsEvent.StartTimeIso?.ToString("o"),
                sportsEvent.Network,
                sportsEvent.Note,
            }),
            JsonSerializerOptions.Web
        );

        var prompt = template
            .Replace("{dates}", string.Join(" and ", days.Select(Day)))
            .Replace("{firstDay}", Day(days[0]))
            .Replace("{count}", "20");

        return $"{prompt}\n\nSUPPLIED EVENTS:\n{eventsJson}";
    }

    private static string Day(DateOnly day) => day.ToString("dddd yyyy-MM-dd", CultureInfo.InvariantCulture);
}
