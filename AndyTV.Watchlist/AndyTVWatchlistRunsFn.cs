using System.Net;
using AndyTV.Watchlist.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AndyTV.Watchlist;

// GET /api/runs?code=<function key> returns a table of recent runs.
public sealed class AndyTVWatchlistRunsFn(BlobStore blobStore)
{
    [Function("runs")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request
    )
    {
        var rows = string.Concat(
            (await blobStore.ReadRuns(60)).Select(run =>
                $"""
                <tr class="{(run.Error is null ? "" : "table-danger")}">
                  <td>{EasternTimeZone.Convert(run.Started):yyyy-MM-dd h:mm tt}</td>
                  <td>{run.Kind}</td>
                  <td>{run.Events}</td>
                  <td>{run.Duration}</td>
                  <td>${run.Cost:0.00}</td>
                  <td>{(run.Feed ? "✓" : "")}</td>
                  <td>{(run.XPostId is null ? "" : $"<a href=\"https://x.com/i/web/status/{Enc(run.XPostId)}\">post</a>")}</td>
                  <td>{(run.InstagramUrl is null ? "" : $"<a href=\"{Enc(run.InstagramUrl)}\">post</a>")}</td>
                  <td><a href="{Enc(blobStore.RunLogUri(run).ToString())}">log</a></td>
                  <td>{Enc(run.Error)}</td>
                </tr>
                """
            )
        );

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/html; charset=utf-8");
        await response.WriteStringAsync(
            $"""
            <!doctype html>
            <html data-bs-theme="dark">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>AndyTV Watchlist Runs</title>
              <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css">
            </head>
            <body class="p-2">
              <div class="table-responsive">
              <table class="table table-sm table-striped text-nowrap">
                <thead><tr><th>Started (ET)</th><th>Kind</th><th>Events</th><th>Duration</th><th>Cost</th><th>Feed</th><th>X</th><th>Instagram</th><th>Log</th><th>Error</th></tr></thead>
                <tbody>{rows}</tbody>
              </table>
              </div>
            </body>
            </html>
            """
        );
        return response;
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
