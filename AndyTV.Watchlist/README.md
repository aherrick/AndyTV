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
ranks and enriches them with one shared prompt (`WatchlistPrompt`). Feed schedules and ESPN's TV
networks are authoritative; only UFC main-card times, golf/tennis times ESPN hasn't confirmed and
missing networks are researched.

Settings: `WATCHLIST_PROMPT` (required; one-line prompt with
`{dates}`, `{firstDay}`, `{count}` placeholders), `COPILOT_GITHUB_TOKEN` (fine-grained PAT with
"Copilot Requests"; optional locally when signed in to Copilot), `GITHUB_DEPLOY_TOKEN` (fine-grained PAT
for `aherrick/AndyTV` with only "Actions: Read and write"; optional, skips the site deploy), plus the existing blob,
Cloudflare, Instagram and X settings.

Each kind saves its site edition (ranked games, picks, plan and full schedule) privately, one blob per date:
`andytv-watchlist-private/site/daily/2026-11-10.json` or `site/weekend/2026-11-13.json` (keyed to the Friday
it ran). Both kinds post to Instagram and X; Friday posts the daily and weekend editions separately.

After the runs, `SitePublishService` reads every saved edition and renders the whole site as static HTML: the
homepage (latest daily, plus the weekend tab Friday-Saturday), an archived copy of each day at `/yyyy/mm/dd/`,
the `/archive/` index and `sitemap.xml`. It zips them into `site.zip` in the public `andytv-watchlist` container
and dispatches the `publish-watchlist-site` workflow, which unzips it into the site and deploys. A deploy
replaces the whole site, so every zip carries the full archive.

`INSTAGRAM_ACCESS_TOKEN` is only the seed: the function refreshes it on each post and keeps
the current token in the private `andytv-watchlist-private` container. After setting a new
seed token, delete `instagram-token.txt` from that container.

Each run saves a JSON summary (events, duration, cost, feed, X/Instagram ids, error) to
`andytv-watchlist-private/runs/`. `GET /api/runs?code=<function key>` shows the latest 60 as a table.

## Processing flow

`AndyTVWatchlistFn` has one timer, an Eastern-hour check, and a Friday condition.
The hour check skips the UTC firing that does not fall at 2 AM Eastern.

`WatchlistPublishingService` carries out each run in this order:

1. `WatchlistResearchService` loads the day's (or weekend's) events; stop if the feeds returned none.
   Otherwise Copilot ranks and enriches them (one retry on a failed session).
2. Build that kind's site edition and save it to private blob storage under its date.
3. Capture each Instagram card, upload them and publish the carousel when configured.
4. Log the X thread and post it when X credentials are present.
