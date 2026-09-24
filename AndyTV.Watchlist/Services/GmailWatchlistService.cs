using System.Net;
using System.Text.Json;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Logging;

namespace AndyTV.Watchlist.Services;

// Reads the newest Daily or Weekend Watchlist email over Gmail IMAP and parses its JSON body.
// Requires the JSON's date to equal the target date so a stale watchlist is never used.
public sealed class GmailWatchlistService(AppSettings settings, ILogger<GmailWatchlistService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<DailyWatchlist?> GetLatest(
        DateOnly targetDate,
        WatchlistKind kind,
        CancellationToken cancellationToken = default
    )
    {
        using var client = new ImapClient();
        await client.ConnectAsync(
            "imap.gmail.com",
            993,
            SecureSocketOptions.SslOnConnect,
            cancellationToken
        );
        await client.AuthenticateAsync(
            settings.GmailAddress,
            settings.GmailAppPassword,
            cancellationToken
        );

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        // Subjects contain either "Daily Watchlist" or "Weekend Watchlist".
        // Keep the searches independent even when both emails arrive on Friday.
        var subject = kind.EmailSubject();
        var query = SearchQuery
            .FromContains(settings.GmailSender)
            .And(SearchQuery.SubjectContains(subject))
            .And(SearchQuery.DeliveredAfter(targetDate.ToDateTime(TimeOnly.MinValue).AddDays(-1)));

        var uids = await inbox.SearchAsync(query, cancellationToken);
        var expectedDate = targetDate.ToString("yyyy-MM-dd");

        try
        {
            // Highest UID is the most recently delivered message; take the newest match for the date.
            foreach (var uid in uids.Reverse())
            {
                var message = await inbox.GetMessageAsync(uid, cancellationToken);
                var json = ExtractJson(message.TextBody ?? message.HtmlBody);
                if (json is null)
                {
                    continue;
                }

                DailyWatchlist? watchlist;
                try
                {
                    watchlist = JsonSerializer.Deserialize<DailyWatchlist>(json, JsonOptions);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Skipping Gmail message {uid}: body was not valid watchlist JSON.", uid);
                    continue;
                }

                if (watchlist is not null && watchlist.Date == expectedDate)
                {
                    logger.LogInformation(
                        "Loaded emailed {kind} watchlist for {date} with {count} games.",
                        kind,
                        watchlist.Date,
                        watchlist.BestWatches.Count
                    );

                    return watchlist;
                }
            }

            return null;
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }

    // The body may be plain text or HTML and can include surrounding prose, so grab the outermost JSON object.
    private static string? ExtractJson(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var decoded = WebUtility.HtmlDecode(body);
        var start = decoded.IndexOf('{');
        var end = decoded.LastIndexOf('}');
        return start >= 0 && end > start ? decoded[start..(end + 1)] : null;
    }
}
