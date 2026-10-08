using System.Net;
using System.Text;
using System.Text.Json;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AndyTV.Watchlist;

// GET /api/feeds?code=<function key> checks the ESPN racing/golf and PGA feeds respond from Azure.
public sealed class AndyTVWatchlistFeedsFn(HttpClient httpClient, PgaTourService golf)
{
    private static readonly string[] EspnUrls =
    [
        "https://site.api.espn.com/apis/site/v2/sports/racing/nascar-premier/scoreboard",
        "https://site.api.espn.com/apis/site/v2/sports/racing/irl/scoreboard",
        "https://site.api.espn.com/apis/site/v2/sports/racing/f1/scoreboard",
        "https://site.api.espn.com/apis/site/v2/sports/golf/pga/scoreboard",
    ];

    [Function("feeds")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request
    )
    {
        var output = new StringBuilder();
        foreach (var url in EspnUrls)
        {
            output.AppendLine(await CheckEspn(url, request.FunctionContext.CancellationToken));
        }

        var today = DateOnly.FromDateTime(EasternTimeZone.Convert(DateTimeOffset.UtcNow).DateTime);
        var pga = await golf.GetEventsForDate(today, request.FunctionContext.CancellationToken);
        output.AppendLine($"BallDontLie PGA {today}: {pga.Count} events {string.Join(", ", pga.Select(e => e.Matchup))}");

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        await response.WriteStringAsync(output.ToString());
        return response;
    }

    private async Task<string> CheckEspn(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var (name, value) in EspnRacingService.BrowserHeaders)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return $"{(int)response.StatusCode} {url}";
            }

            using var document = JsonDocument.Parse(body);
            var events = document.RootElement.GetProperty("events").EnumerateArray()
                .Select(e => $"{e.GetProperty("name").GetString()} ({e.GetProperty("date").GetString()})");
            return $"{(int)response.StatusCode} {url} {body.Length:N0} chars: {string.Join(", ", events)}";
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException)
        {
            return $"ERROR {url}: {ex.Message}";
        }
    }
}
