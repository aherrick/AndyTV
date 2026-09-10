using System.Globalization;
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
            "Golf" => "⛳",
            "Tennis" => "🎾",
            _ => "📺",
        };

    public static string Time(DateTimeOffset value) =>
        $"{EasternTimeZone.Convert(value).ToString("h:mm tt", CultureInfo.InvariantCulture)} ET";

    // Best-per-category picks, shared by the X post and the site's Top tab. Games arrive in rank order.
    public static List<(string Icon, string Label, WatchlistGame Game)> TopPicks(
        IReadOnlyList<WatchlistGame> games
    )
    {
        var picks = new List<(string Icon, string Label, WatchlistGame Game)>();

        void Add(string icon, string label, IEnumerable<WatchlistGame> source)
        {
            if (source.FirstOrDefault() is { } game)
            {
                picks.Add((icon, label, game));
            }
        }

        Add("🔥", "Best overall", games);
        Add("🏈", "Best football", games.Where(game => game.Sport == "Football"));
        Add("⚾", "Best baseball", games.Where(game => game.Sport == "Baseball"));
        Add("🏒", "Best hockey", games.Where(game => game.Sport == "Hockey"));
        Add("🏀", "Best basketball", games.Where(game => game.Sport == "Basketball"));
        Add("⚽", "Best soccer", games.Where(game => game.Sport == "Soccer"));
        Add("⛳", "Best golf", games.Where(game => game.Sport == "Golf"));
        Add(
            "🌙",
            "Best late-night",
            games.Where(game => EasternTimeZone.Convert(game.StartTimeIso).Hour >= 22)
        );

        return picks;
    }
}
