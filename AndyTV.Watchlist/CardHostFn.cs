using AndyTV.Watchlist.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AndyTV.Watchlist;

// Serves the rendered card PNGs straight off the Function's per-day storage folders.
public class CardHostFn
{
    [Function("CardImage")]
    public IActionResult Image(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cards/{date}/{name}")]
            HttpRequest req,
        string date,
        string name
    )
    {
        var safeName = Path.GetFileName(name);
        if (!IsDayFolder(date) || !safeName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            return new NotFoundResult();
        }

        var path = Path.Combine(CardStorage.Root, date, safeName);
        return File.Exists(path) ? new PhysicalFileResult(path, "image/png") : new NotFoundResult();
    }

    [Function("CardIndex")]
    public IActionResult Index(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cards/{date}")] HttpRequest req,
        string date
    )
    {
        if (!IsDayFolder(date))
        {
            return new NotFoundResult();
        }

        var dir = Path.Combine(CardStorage.Root, date);
        string[] files = Directory.Exists(dir)
            ? [.. Directory.GetFiles(dir, "*.png").Select(Path.GetFileName).OfType<string>().Order()]
            : [];

        var images = string.Concat(
            files.Select(file =>
                $"<img src=\"/api/cards/{date}/{file}\" style=\"width:100%;max-width:540px;display:block;margin:12px auto\">"
            )
        );

        var body =
            "<!doctype html><meta name=\"viewport\" content=\"width=device-width\">"
            + "<body style=\"margin:0;background:#061524\">"
            + images
            + "</body>";

        return new ContentResult
        {
            Content = body,
            ContentType = "text/html",
        };
    }

    private static bool IsDayFolder(string date) => date.Length == 8 && date.All(char.IsAsciiDigit);
}
