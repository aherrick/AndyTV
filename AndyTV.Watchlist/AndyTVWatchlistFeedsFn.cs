using System.Net;
using System.Text;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AndyTV.Watchlist;

// GET /api/feeds lists today's ESPN events as the watchlist sees them; failed feeds are logged as warnings.
public sealed class AndyTVWatchlistFeedsFn(EspnService espn)
{
    [Function("feeds")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request
    )
    {
        var today = EasternTimeZone.Today;
        var events = await espn.GetEventsForDate(today, request.FunctionContext.CancellationToken);

        var output = new StringBuilder($"{today:yyyy-MM-dd}: {events.Count} events\n");
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
