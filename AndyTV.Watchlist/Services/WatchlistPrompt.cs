using System.Globalization;
using System.Text.Json;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

// The WATCHLIST_PROMPT app setting; {dates}, {firstDay} and {count} are filled in and the supplied events appended.
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
                sportsEvent.Betting,
                sportsEvent.SourceUrl,
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
