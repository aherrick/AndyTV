using AndyTV.Watchlist.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;

namespace AndyTV.Watchlist;

public sealed class WatchlistScoresFn(WatchlistScoreService scores)
{
    [Function(nameof(WatchlistScoresFn))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scores")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        request.HttpContext.Response.Headers.CacheControl = "no-store";
        return new JsonResult(await scores.GetAsync(cancellationToken), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
