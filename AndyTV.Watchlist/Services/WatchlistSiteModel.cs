namespace AndyTV.Watchlist.Services;

// Render-ready shape serialized to latest.json and consumed by the static site's app.js.
public sealed record WatchlistSiteModel(
    string Date,
    string Updated,
    TopTab Top,
    List<Game> Timeline,
    PlanTab Plan
);

public sealed record TopTab(List<TopPick> Picks, List<Game> Games);

public sealed record TopPick(string Icon, string Label, string Matchup, string Time, string TimeIso);

public sealed record Game(
    int Rank,
    string Icon,
    string Matchup,
    string Time,
    string TimeIso,
    string Network,
    string League,
    string Reason
);

public sealed record PlanTab(string Summary, List<PlanStep> Steps);

public sealed record PlanStep(
    string TimeIso,
    string Time,
    string Icon,
    string Matchup,
    string Instruction,
    List<PlanAlt> Alternates
);

public sealed record PlanAlt(string Icon, string Matchup);
