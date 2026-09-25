# Watchlist email schedule

One function runs at 3:30 AM Eastern every day. It processes Daily first, then
also processes Weekend on Fridays. A missing Daily email does not skip the Weekend check.
The timer uses 07:30 and 08:30 UTC with an Eastern-hour guard for daylight saving time.
The app runs on Linux Flex Consumption, where Azure does not support
[`WEBSITE_TIME_ZONE`](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#ncrontab-time-zones).

Emails are selected by sender and subject containing `Daily Watchlist` or
`Weekend Watchlist`, respectively (the legacy `GMAIL_SUBJECT` setting is no longer used).
Both use the same JSON structure, with `date` matching the run date (Friday for the
weekend email). The newest valid matching email is used; stale dates are skipped.

Daily publishes `latest.json`; weekend publishes `latest_weekend.json` for the
site's Weekend tab. Local previews use the same filenames under `publish/`.
Weekend processing only publishes the data feed; social posts remain daily.
Sunday's 3:30 AM Eastern daily run deletes `latest_weekend.json` before checking
email, including when no daily email is available. Missing files are harmless;
local preview mode deletes only `publish/latest_weekend.json`.

## Processing flow

`AndyTVWatchlistFn` has one timer, an Eastern-hour check, and a Friday condition.
The hour check skips the UTC firing that does not fall at 3 AM Eastern.
Debug and Release use the same schedule; neither requests an extra startup run.

`WatchlistPublishingService` carries out each accepted run in this order:

1. On Sunday, delete the weekend feed before contacting Gmail.
2. Read the newest email for the requested kind and date. Stop if it has no games.
3. Build the site's JSON model.
4. Save that kind's feed under `publish/` in local mode, or publish it to blob
   storage when configured. Weekend processing ends here.
5. For Daily, capture each Instagram card once. Local mode saves the PNGs under
   `publish/insta/`; otherwise upload them and publish the carousel when configured.
6. Log the daily X thread and post it when X credentials are present.

`WatchlistKind` keeps the email subject and feed filename mappings in one place.
Local mode skips blob uploads and Instagram publishing. As before, X posting is
controlled separately by the four X credentials, including in local mode.
