using System.Globalization;
using System.Net;
using AndyTV.Watchlist.Models;

namespace AndyTV.Watchlist.Services;

// Renders the whole single-page site straight from the emailed DailyWatchlist, filling the
// index template's Top / Timeline / Plan tab bodies.
public static class WatchlistSiteBuilder
{
    private static readonly string Template = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "assets", "templates", "site", "index.template.html")
    );

    public static string BuildHtml(DailyWatchlist watchlist, DateOnly targetDate)
    {
        var games = watchlist.BestWatches.OrderBy(game => game.Rank).ToList();

        return Template
            .Replace(
                "{{DATE}}",
                Enc(targetDate.ToString("dddd, MMMM d", CultureInfo.InvariantCulture))
            )
            .Replace("{{TOP}}", TopBody(games))
            .Replace("{{TIMELINE}}", TimelineBody(games))
            .Replace("{{PLAN}}", PlanBody(watchlist));
    }

    private static string TimelineBody(List<WatchlistGame> games)
    {
        var rows = string.Concat(
            games
                .OrderBy(game => game.StartTimeIso)
                .Select(game =>
                    $"""
                    <li>
                      <div class="row-time">{Enc(SportsFormat.Time(game.StartTimeIso))}</div>
                      <div>
                        <div class="row-game">{SportsFormat.Icon(game.Sport)} {Enc(game.Matchup)}</div>
                        <div class="row-meta">#{game.Rank} overall · {Enc(game.League)}{Network(game)}</div>
                      </div>
                    </li>
                    """
                )
        );

        return $"<ul class=\"rows\">{rows}</ul>";
    }

    private static string TopBody(List<WatchlistGame> games)
    {
        var rows = string.Concat(
            games.Select(game =>
                $"""
                <li class="game">
                  <div class="rank">{game.Rank}</div>
                  <div class="body">
                    <div class="title">{SportsFormat.Icon(game.Sport)} {Enc(game.Matchup)}</div>
                    <div class="meta">{Enc(SportsFormat.Time(game.StartTimeIso))}{Network(game)} · {Enc(game.League)}</div>
                    <div class="desc">{Enc(game.Reason.Trim())}</div>
                  </div>
                </li>
                """
            )
        );

        return $"{TopPicksBody(games)}<ol class=\"games\">{rows}</ol>";
    }

    private static string TopPicksBody(List<WatchlistGame> games)
    {
        var picks = SportsFormat.TopPicks(games);

        if (picks.Count == 0)
        {
            return "";
        }

        var items = string.Concat(
            picks.Select(pick =>
                $"<li>{pick.Icon} <strong>{Enc(pick.Label)}:</strong> {Enc(pick.Game.Matchup)} · {Enc(SportsFormat.Time(pick.Game.StartTimeIso))}</li>"
            )
        );

        return $"<h3 class=\"summary-title top-picks-title\">⭐ Top Picks</h3><ul class=\"summary\">{items}</ul>";
    }

    private static string PlanBody(DailyWatchlist watchlist)
    {
        var steps = SportsFormat.WatchPlanSteps(watchlist);

        if (steps.Count == 0)
        {
            return "";
        }

        var rows = string.Concat(
            steps.Select(step =>
            {
                var matchup = step.Matchup.Length == 0 ? "" : $"{Enc(step.Matchup)} — ";
                return $"<li>{step.Icon} {Enc(SportsFormat.Time(step.Time))} {matchup}{Enc(step.Instruction)}</li>";
            })
        );

        var summary = string.IsNullOrWhiteSpace(watchlist.WatchPlan!.Summary)
            ? ""
            : $"<div class=\"plan-summary\"><strong>Game Plan</strong>{Enc(watchlist.WatchPlan.Summary.Trim())}</div>";

        return $"{summary}<h3 class=\"summary-title\">The Watch Plan</h3><ul class=\"summary\">{rows}</ul>";
    }

    private static string Network(WatchlistGame game) =>
        string.IsNullOrWhiteSpace(game.Network)
            ? ""
            : $" · <span class=\"net\">{Enc(game.Network.Trim())}</span>";

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
