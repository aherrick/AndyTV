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

// Reads the newest "AndyTV Daily Watchlist JSON" email over Gmail IMAP and parses its JSON body.
// Requires the JSON's date to equal the target date so a stale watchlist is never used.
public sealed class GmailWatchlistService(AppSettings settings, ILogger<GmailWatchlistService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<DailyWatchlist?> GetLatest(
        DateOnly targetDate,
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

        var query = SearchQuery
            .FromContains(settings.GmailSender)
            .And(SearchQuery.SubjectContains(settings.GmailSubject))
            .And(SearchQuery.DeliveredAfter(DateTime.Today.AddDays(-1)));

        var uids = await inbox.SearchAsync(query, cancellationToken);

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

                if (watchlist is not null && watchlist.Date == targetDate.ToString("yyyy-MM-dd"))
                {
                    logger.LogInformation(
                        "Loaded emailed watchlist for {date} with {count} games.",
                        watchlist.Date,
                        watchlist.BestWatches.Count
                    );
                    return watchlist;
                }
            }

            logger.LogInformation("No matching watchlist email found for {date}.", targetDate);
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
