using System.Globalization;
using System.Net;
using System.Text;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AndyTV.Watchlist;

// GET /api/feeds?pin=<FEEDS_PIN>[&date=yyyyMMdd] lists that day's ESPN events (default today ET) as the watchlist
// sees them; failed feeds are logged as warnings. The pin keeps strangers from burning ESPN requests from our IP.
public sealed class AndyTVWatchlistFeedsFn(EspnService espn, AppSettings settings)
{
    [Function("feeds")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request
    )
    {
        if (string.IsNullOrEmpty(settings.FeedsPin) || request.Query["pin"] != settings.FeedsPin)
        {
            return request.CreateResponse(HttpStatusCode.Unauthorized);
        }

        var date = EasternTimeZone.Today;
        var query = request.Query["date"];
        if (
            !string.IsNullOrEmpty(query)
            && !DateOnly.TryParseExact(query, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
        )
        {
            var badRequest = request.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("date must be yyyyMMdd, e.g. 20261010");
            return badRequest;
        }

        var events = await espn.GetEventsForDate(date, request.FunctionContext.CancellationToken);

        var output = new StringBuilder($"{date:yyyy-MM-dd}: {events.Count} events\n");
        foreach (var e in events.OrderBy(e => e.StartTimeIso))
        {
            var time = e.StartTimeIso?.ToString("h:mm tt") ?? "TBD";
            var network = e.Network is null ? "" : $" | {e.Network}";
            output.AppendLine($"{time} | {e.Sport} | {e.League} | {e.Matchup}{network}");
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        await response.WriteStringAsync(output.ToString());
        return response;
    }
}
