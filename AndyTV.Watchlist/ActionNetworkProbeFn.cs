using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AndyTV.Watchlist;

// Temporary open probe: GET /api/odds-probe?sport=mlb&date=20261007 relays the Action Network JSON to test Azure IP access.
public sealed class ActionNetworkProbeFn(HttpClient http)
{
    [Function("odds-probe")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request
    )
    {
        var query = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
        var sport = Uri.EscapeDataString(query["sport"] ?? "mlb");
        var date = Uri.EscapeDataString(query["date"] ?? DateTime.UtcNow.AddHours(-4).ToString("yyyyMMdd"));

        using var upstream = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.actionnetwork.com/web/v2/scoreboard/{sport}?bookIds=69&date={date}&periods=event"
        );
        upstream.Headers.UserAgent.ParseAdd("Mozilla/5.0");

        using var result = await http.SendAsync(upstream);
        var body = await result.Content.ReadAsStringAsync();

        var response = request.CreateResponse(result.StatusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(body);
        return response;
    }
}
