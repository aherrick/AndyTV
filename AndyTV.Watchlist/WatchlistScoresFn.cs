using AndyTV.Watchlist.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;

namespace AndyTV.Watchlist;

public sealed class WatchlistScoresFn(WatchlistScoreService scores)
{
    private const string AllowedHost = "andytv.today";

    [Function(nameof(WatchlistScoresFn))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "scores")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        // Reject callers that aren't the site before doing any score work.
        if (!IsAllowed(request))
        {
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        request.HttpContext.Response.Headers.CacheControl = "no-store";
        return new JsonResult(await scores.GetAsync(cancellationToken), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static bool IsAllowed(HttpRequest request)
    {
        var source = request.Headers.Origin.FirstOrDefault() ?? request.Headers.Referer.FirstOrDefault();
        return source is not null
            && Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && (uri.Host.Equals(AllowedHost, StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith("." + AllowedHost, StringComparison.OrdinalIgnoreCase));
    }
}
