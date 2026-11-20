using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using AndyTV.Watchlist.Configuration;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AndyTV.Watchlist;

// Experiment: GET /api/usage?pin=<FEEDS_PIN> asks ChatGPT's usage endpoint what the saved session sees and
// returns the status plus the usage fields. Each call refreshes the session, so the pin is required.
public sealed class AndyTVWatchlistUsageFn(ChatGptClient chatGpt, HttpClient http, AppSettings settings)
{
    [Function("usage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request
    )
    {
        if (string.IsNullOrEmpty(settings.FeedsPin) || request.Query["pin"] != settings.FeedsPin)
        {
            return request.CreateResponse(HttpStatusCode.Unauthorized);
        }

        var cancellationToken = request.FunctionContext.CancellationToken;
        using var usage = new HttpRequestMessage(HttpMethod.Get, "https://chatgpt.com/backend-api/wham/usage")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", await chatGpt.RefreshAccessToken(cancellationToken)) },
        };
        using var result = await http.SendAsync(usage, cancellationToken);
        var body = await result.Content.ReadAsStringAsync(cancellationToken);

        // Keep the account's identity out of the response.
        if (result.IsSuccessStatusCode && JsonNode.Parse(body) is JsonObject json)
        {
            foreach (var key in new[] { "user_id", "account_id", "email" })
            {
                json.Remove(key);
            }
            body = json.ToJsonString(new() { WriteIndented = true });
        }

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        await response.WriteStringAsync($"HTTP {(int)result.StatusCode} {result.StatusCode}\n{body[..Math.Min(body.Length, 4000)]}");
        return response;
    }
}
