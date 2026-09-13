# ESPN score enrichment

The daily function enriches games before building `latest.json`, using Refit.HttpClientFactory
(which includes Refit) and Raffinert.FuzzySharp. Add structured team names to each emailed
`bestWatches` entry, alongside the existing fields:

```json
{
  "awayTeam": "Indianapolis Colts",
  "homeTeam": "Houston Texans",
  "matchup": "Indianapolis Colts @ Houston Texans"
}
```

Team identities should come from the schedule source before ranking. The email producer is
external to this repository and must supply these fields; older emails still render but skip
matching. Matchup remains display text and is never parsed.

Supported leagues: NFL, NCAAF, MLB, NBA, WNBA, NHL, and soccer. Matching requires both teams
to score at least 80 within 60 minutes of the scheduled start; tied candidates are skipped.
ESPN failures leave the daily guide available without scores.

The daily feed includes `sport`, `awayTeam`, `homeTeam`, and a score snapshot.
No ESPN IDs are stored or reused; each refresh matches the structured teams again.

## Live scores

`GET /api/scores` returns only matched games from the 20 highest-ranked entries in
`latest.json` that ESPN marks as live (`state: "in"`). Scheduled, finished, unsupported,
and unmatched games are omitted. An empty `games` array means no live matches were
available; ESPN failures are logged and affected games are omitted rather than returning
old scores. A missing/unreadable daily feed returns HTTP 503.

```json
{
  "checkedAt": "2026-09-13T18:00:00Z",
  "games": [{
    "rank": 1,
    "matchup": "Indianapolis Colts @ Houston Texans",
    "league": "NFL",
    "awayTeam": "Indianapolis Colts",
    "homeTeam": "Houston Texans",
    "score": { "away": "7", "home": "10", "state": "in", "detail": "2nd Quarter", "updatedAt": "2026-09-13T18:00:00Z" }
  }]
}
```

The function and console both call `WatchlistPollingService.GetAsync`. It reloads the daily
feed and ESPN scores at most once every 30 seconds per running instance, including concurrent
requests. This is an in-memory response cache, not stored event matching. The endpoint reads
the same public blob as the static site by default; override `WATCHLIST_FEED` with a feed URL
or local `latest.json` path. It never writes blobs or posts to social accounts.

The endpoint is anonymous so a static site can poll it without embedding a function key.
When deploying, allow the static site's origin in the Function App's CORS settings. The
existing static UI is unchanged; it can poll this endpoint every 30 seconds and use `rank`
to attach live scores to its daily list. Poll results are a separate live-only list, not a
replacement for the complete daily feed.

## Console polling test

From the repository root, poll the published feed (no Functions host or secrets required):

```powershell
dotnet run --project AndyTV.Watchlist.Console
```

To use a local generated feed, set `WATCHLIST_FEED` before running:

```powershell
$env:WATCHLIST_FEED = "./publish/latest.json"
dotnet run --project AndyTV.Watchlist.Console
```

The console prints the response as JSON every 30 seconds, calling the same service directly and fetching only the daily JSON
and ESPN. It never calls `/api/scores`. Press Ctrl+C to stop. Older feeds without structured
team names return no live matches; regenerate them with `awayTeam`, `homeTeam`, and `sport`
(needed to route soccer leagues).
