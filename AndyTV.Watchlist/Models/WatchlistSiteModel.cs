namespace AndyTV.Watchlist.Models;

// Render-ready shape serialized to latest.json and read back by SitePageRenderer.
public sealed record WatchlistSiteModel(
    string Date,
    string Updated,
    TopTab Top,
    PlanTab Plan
);

public sealed record TopTab(List<TopPick> Picks, List<SiteGame> Games);

public sealed record TopPick(string Icon, string Label, string Matchup, string Time, string TimeIso);

public sealed record SiteGame(
    int Rank,
    string Icon,
    string Matchup,
    string Time,
    string TimeIso,
    string Network,
    string League,
    string Reason,
    string Sport,
    List<WatchSource> Sources,
    List<string> Odds
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

// Every feed event for the edition, serialized to latest_all.json / latest_weekend_all.json for the All views.
public sealed record ScheduleEvent(string Time, string TimeIso, string Icon, string Sport, string League, string Matchup, string Network, int? Rank);

// One published edition (Daily or Weekend) as the page renderer needs it.
public sealed record SiteEdition(WatchlistSiteModel Model, List<ScheduleEvent> Schedule);
