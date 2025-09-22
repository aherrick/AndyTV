using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;

// helpers
static string Escape(string? s) =>
    (s ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " ").Trim();

// top-level async main
var shows = await RefreshGuide();

var path = Path.Combine(AppContext.BaseDirectory, "guide.csv");
using var sw = new StreamWriter(path);
sw.WriteLine("ChannelName\tStart\tEnd\tTitle\tDescription");
foreach (var s in shows.OrderBy(x => x.ChannelName).ThenBy(x => x.Start))
{
    sw.WriteLine(
        $"{s.ChannelName}\t{s.Start:o}\t{s.End:o}\t{Escape(s.Title)}\t{Escape(s.Description)}"
    );
}
Console.WriteLine($"Done. {shows.Count} rows -> {path}");

// ------------------- Methods -------------------

static async Task<List<Guide>> RefreshGuide()
{
    var tvChannelFavs = GetTVChannels();
    var shows = new List<Guide>();

    var pstTz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

    foreach (
        var tvChannelFav in tvChannelFavs.Where(x => !string.IsNullOrWhiteSpace(x.StreamingTVId))
    )
    {
        await Task.Delay(1500); // polite delay

        Console.WriteLine($"Scraping {tvChannelFav.ChannelName}");

        var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
        var document = await context.OpenAsync(
            $"https://streamingtvguides.com/Channel/{tvChannelFav.StreamingTVId}"
        );

        foreach (var showHtml in document.QuerySelectorAll(".card-body"))
        {
            var title =
                showHtml
                    .QuerySelector("h5")
                    ?.TextContent.Replace("Playing Now!", "", StringComparison.OrdinalIgnoreCase)
                    .Trim() ?? "";

            var sub = showHtml.QuerySelector("h6")?.TextContent ?? "";
            if (!string.IsNullOrWhiteSpace(sub))
            {
                title += " - " + sub.Trim();
            }

            var timeLine = showHtml
                .ChildNodes.OfType<IText>()
                .Select(m => m.Text.Trim())
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                ?.Replace("\n", "")
                ?.Replace("PST", "", StringComparison.OrdinalIgnoreCase)
                ?.Replace("PDT", "", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(timeLine) || !timeLine.Contains('-'))
                continue;

            var parts = timeLine.Split(" - ", StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                continue;

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(parts[0]), pstTz);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(parts[1]), pstTz);

            var desc = showHtml.QuerySelector("p.card-text")?.TextContent?.Trim() ?? "";

            var showDb = new Guide
            {
                StreamingTVId = tvChannelFav.StreamingTVId,
                ChannelName = tvChannelFav.ChannelName,
                Category = tvChannelFav.Category,
                Title = title,
                Start = startUtc,
                End = endUtc,
                Description = desc,
            };

            var exists = shows.FirstOrDefault(p =>
                p.Title == showDb.Title && p.Start == showDb.Start
            );
            if (exists == null && showDb.Start > DateTime.UtcNow.AddHours(-6))
            {
                shows.Add(showDb);
            }
        }
    }

    return shows;
}

static List<TVChannel> GetTVChannels() =>
    new()
    {
        // Sports
        new()
        {
            ChannelName = "ESPN",
            Category = "Sports",
            StreamingTVId = "ESPN",
        },
        new()
        {
            ChannelName = "ESPN2",
            Category = "Sports",
            StreamingTVId = "ESPN2",
        },
        new()
        {
            ChannelName = "ESPNews",
            Category = "Sports",
            StreamingTVId = "ESPNEWS",
        },
        new()
        {
            ChannelName = "ESPNU",
            Category = "Sports",
            StreamingTVId = "ESPNU",
        },
        new()
        {
            ChannelName = "FS1",
            Category = "Sports",
            StreamingTVId = "FS1",
        },
        new()
        {
            ChannelName = "FS2",
            Category = "Sports",
            StreamingTVId = "FS2",
        },
        new()
        {
            ChannelName = "NFL Network",
            Category = "Sports",
            StreamingTVId = "NFLNET",
        },
        new()
        {
            ChannelName = "NBA TV",
            Category = "Sports",
            StreamingTVId = "NBATVHD",
        },
        new()
        {
            ChannelName = "MLB Network",
            Category = "Sports",
            StreamingTVId = "MLBHD",
        },
        new()
        {
            ChannelName = "Golf Channel",
            Category = "Sports",
            StreamingTVId = "GOLF",
        },
        // News
        new()
        {
            ChannelName = "Newsmax",
            Category = "News",
            StreamingTVId = "NEWSMXH",
        },
        new()
        {
            ChannelName = "Fox News",
            Category = "News",
            StreamingTVId = "FNCHD",
        },
        new()
        {
            ChannelName = "Fox Business",
            Category = "News",
            StreamingTVId = "FBN",
        },
        new()
        {
            ChannelName = "CNN",
            Category = "News",
            StreamingTVId = "CNNHD",
        },
        new()
        {
            ChannelName = "MSNBC",
            Category = "News",
            StreamingTVId = "MSNBC",
        },
        new()
        {
            ChannelName = "CNBC",
            Category = "News",
            StreamingTVId = "CNBC",
        },
        // Entertainment
        new()
        {
            ChannelName = "BBC America",
            Category = "Entertainment",
            StreamingTVId = "BBCA",
        },
        new()
        {
            ChannelName = "USA Network",
            Category = "Entertainment",
            StreamingTVId = "USA",
        },
        new()
        {
            ChannelName = "E!",
            Category = "Entertainment",
            StreamingTVId = "EHD",
        },
        new()
        {
            ChannelName = "History",
            Category = "Entertainment",
            StreamingTVId = "HSTRYHD",
        },
        new()
        {
            ChannelName = "Discovery Channel",
            Category = "Entertainment",
            StreamingTVId = "DSCHD",
        },
        new()
        {
            ChannelName = "Science Channel",
            Category = "Entertainment",
            StreamingTVId = "SCIHD",
        },
        new()
        {
            ChannelName = "Smithsonian",
            Category = "Entertainment",
            StreamingTVId = "SMTHHD",
        },
        new()
        {
            ChannelName = "Travel Channel",
            Category = "Entertainment",
            StreamingTVId = "TRVL",
        },
        new()
        {
            ChannelName = "DIY Network",
            Category = "Entertainment",
            StreamingTVId = "DIY",
        },
        new()
        {
            ChannelName = "HGTV",
            Category = "Entertainment",
            StreamingTVId = "HGTV",
        },
        new()
        {
            ChannelName = "TLC",
            Category = "Entertainment",
            StreamingTVId = "TLC",
        },
        new()
        {
            ChannelName = "Bravo",
            Category = "Entertainment",
            StreamingTVId = "BRAVO",
        },
        new()
        {
            ChannelName = "Food Network",
            Category = "Entertainment",
            StreamingTVId = "FOOD",
        },
        new()
        {
            ChannelName = "truTV",
            Category = "Entertainment",
            StreamingTVId = "TRUTV",
        },
        // Movies / Premium
        new()
        {
            ChannelName = "HBO",
            Category = "Movies",
            StreamingTVId = "HBO",
        },
        new()
        {
            ChannelName = "HBO 2",
            Category = "Movies",
            StreamingTVId = "HBO2",
        },
        new()
        {
            ChannelName = "HBO Signature",
            Category = "Movies",
            StreamingTVId = "HBOSGHD",
        },
        new()
        {
            ChannelName = "HBO Zone",
            Category = "Movies",
            StreamingTVId = "HBOZHD",
        },
        new()
        {
            ChannelName = "Starz",
            Category = "Movies",
            StreamingTVId = "STARZ",
        },
        new()
        {
            ChannelName = "Starz Encore",
            Category = "Movies",
            StreamingTVId = "STZENHD",
        },
        new()
        {
            ChannelName = "Cinemax",
            Category = "Movies",
            StreamingTVId = "MAX",
        },
        new()
        {
            ChannelName = "The Movie Channel",
            Category = "Movies",
            StreamingTVId = "TMC",
        },
    };

// ------------------- Models -------------------

public class TVChannel
{
    public string ChannelName { get; set; } = "";
    public string Category { get; set; } = "";
    public string StreamingTVId { get; set; } = "";
}

public class Guide
{
    public string StreamingTVId { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Description { get; set; } = "";
}