# Watchlist schedule

One function runs at 2:30 AM Eastern every day. It researches and publishes Daily,
and on Fridays researches Weekend (Saturday + Sunday) at the same time.
The timer uses 06:30 and 07:30 UTC with an Eastern-hour guard for daylight saving time.
The app runs on Linux Flex Consumption, where Azure does not support
[`WEBSITE_TIME_ZONE`](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#ncrontab-time-zones).

The watchlist is produced end to end in the function: ESPN's public scoreboards
(MLB, NFL, NHL, NBA/WNBA, college football and basketball, curated soccer, UFC, F1,
NASCAR Cup, IndyCar, PGA Tour, Grand Slam tennis) supply the candidate events, then GitHub Copilot
(`gpt-6.1-sol`, web search/fetch only)
ranks and enriches them with one shared prompt (`WatchlistPrompt`). Feed schedules are
authoritative and validated; only UFC main-card times and golf times ESPN hasn't confirmed are researched.

Settings: `WATCHLIST_PROMPT` (required; one-line prompt with
`{dates}`, `{firstDay}`, `{count}` placeholders), `COPILOT_GITHUB_TOKEN` (fine-grained PAT with
"Copilot Requests"; optional locally when signed in to Copilot), plus the existing blob,
Cloudflare, Instagram and X settings.

Daily publishes `latest.json`; weekend publishes `latest_weekend.json` for the
site's Weekend tab. Both kinds post to Instagram and X; Friday posts the daily and
weekend editions separately. Sunday's daily run deletes `latest_weekend.json` first.

`INSTAGRAM_ACCESS_TOKEN` is only the seed: the function refreshes it on each post and keeps
the current token in the private `andytv-watchlist-private` container. After setting a new
seed token, delete `instagram-token.txt` from that container.

Each run saves a JSON summary (events, duration, cost, feed, X/Instagram ids, error) to
`andytv-watchlist-private/runs/`. `GET /api/runs?code=<function key>` shows the latest 60 as a table.

## Processing flow

`AndyTVWatchlistFn` has one timer, an Eastern-hour check, and a Friday condition.
The hour check skips the UTC firing that does not fall at 2 AM Eastern.

`WatchlistPublishingService` carries out each run in this order:

1. On Sunday, delete the weekend feed.
2. `WatchlistResearchService` loads the day's (or weekend's) events, runs the prompt
   and validates the result. Stop if the feeds returned no events.
3. Build the site's JSON model and publish that kind's feed to blob storage.
4. Capture each Instagram card, upload them and publish the carousel when configured.
5. Log the X thread and post it when X credentials are present.
