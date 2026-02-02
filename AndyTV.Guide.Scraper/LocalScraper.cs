using System.Globalization;
using AndyTV.Data.Models;
using AngleSharp;
using AngleSharp.Dom;

namespace AndyTV.Guide.Scraper;

public static class LocalScraper
{
    public static async Task<List<Show>> GetLocalGuide()
    {
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);

        var networks = new[]
        {
            (Slug: "fox", Name: "FOX"),
            (Slug: "cbs", Name: "CBS"),
            (Slug: "nbc", Name: "NBC"),
            (Slug: "abc", Name: "ABC"),
            (Slug: "the-cw", Name: "The CW"),
        };

        var allShows = new List<Show>();

        foreach (var network in networks)
        {
            try
            {
                var shows = await ScrapeNetwork(context, network.Slug, network.Name);
                Console.WriteLine($"Found {shows.Count} shows for {network.Name}");
                allShows.AddRange(shows);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping {network.Name}: {ex.Message}");
            }
        }

        return allShows;
    }

    private static async Task<List<Show>> ScrapeNetwork(
        IBrowsingContext context,
        string slug,
        string channelName
    )
    {
        var estTz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var url = $"https://www.tvinsider.com/network/{slug}/schedule/";
        Console.WriteLine($"Scraping {url}...");

        var document = await context.OpenAsync(url);
        var shows = new List<Show>();

        // Find all date headers (e.g. <h2 id="02-01-2026" class="date">Sunday, February 1</h2>)
        var dateHeaders = document.QuerySelectorAll("h2.date");

        if (dateHeaders.Length == 0)
        {
            Console.WriteLine(
                $"No date headers found for {channelName}. Structure might have changed."
            );
            return shows;
        }

        foreach (var header in dateHeaders)
        {
            var dateId = header.Id; // e.g. "02-01-2026"

            // Parse the date from the ID
            if (
                !DateTime.TryParseExact(
                    dateId,
                    "MM-dd-yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var datePart
                )
            )
            {
                Console.WriteLine($"Could not parse date from id: {dateId}");
                continue;
            }

            // Find the associated show grid (sibling)
            var sibling = header.NextElementSibling;
            while (
                sibling?.ClassList.Contains("show-grid") is false
                && sibling?.TagName.Equals("H2", StringComparison.OrdinalIgnoreCase) is false
            )
            {
                sibling = sibling.NextElementSibling;
            }

            if (sibling?.ClassList.Contains("show-grid") == true)
            {
                var showElements = sibling.QuerySelectorAll("a.show-upcoming");
                foreach (var showEl in showElements)
                {
                    var timeEl = showEl.QuerySelector("time");
                    var titleEl = showEl.QuerySelector("h3");

                    var typeYearEl = showEl.QuerySelector("h4"); // e.g. "Series • 2026"
                    var epTitleEl = showEl.QuerySelector("h5");  // e.g. "When You Know, You Know"
                    var seasonEpEl = showEl.QuerySelector("h6"); // e.g. "Season 2 • Episode 14"
                    var descEl = showEl.QuerySelector("p");

                    // Need at least time and title
                    if (timeEl == null || titleEl == null)
                    {
                        continue;
                    }

                    // Clean title: remove "New" if present
                    var rawTitle = "";
                    foreach(var node in titleEl.ChildNodes)
                    {
                        if (node is IText textNode)
                        {
                            rawTitle += textNode.Text;
                        }
                    }
                    rawTitle = rawTitle.Trim();
                    if (string.IsNullOrEmpty(rawTitle))
                    {
                        rawTitle = titleEl.TextContent.Replace("New", "").Trim();
                    }

                    // Construct Subject
                    var subject = rawTitle;
                    var epTitle = epTitleEl?.TextContent?.Trim();
                    if (!string.IsNullOrWhiteSpace(epTitle))
                    {
                        subject += $" - {epTitle}";
                    }

                    // Construct Description
                    var descParts = new List<string>();
                    if (typeYearEl != null) descParts.Add(typeYearEl.TextContent.Trim());
                    if (seasonEpEl != null) descParts.Add(seasonEpEl.TextContent.Trim());
                    
                    var metaInfo = string.Join(" | ", descParts);
                    var bodyDesc = descEl?.TextContent?.Trim() ?? "";

                    var finalDesc = string.IsNullOrWhiteSpace(metaInfo)
                        ? bodyDesc
                        : $"{metaInfo}\n{bodyDesc}";

                    var timeText = timeEl.TextContent.Trim();
                    var combinedDateString = $"{dateId} {timeText}";

                    if (
                        DateTime.TryParseExact(
                            combinedDateString,
                            "MM-dd-yyyy h:mm tt",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var estStartTime
                        )
                    )
                    {
                        var show = new Show
                        {
                            StreamingTVId = slug,
                            ChannelName = channelName,
                            Category = "LOCAL",
                            Subject = subject,
                            StartTime = TimeZoneInfo.ConvertTimeToUtc(estStartTime, estTz),
                            Description = finalDesc.Trim(),
                        };
                        shows.Add(show);
                    }
                }
            }
        }

        // Second pass: Calculate EndTime
        shows = shows.OrderBy(s => s.StartTime).ToList();

        for (int i = 0; i < shows.Count; i++)
        {
            var currentShow = shows[i];

            if (i < shows.Count - 1)
            {
                var nextShow = shows[i + 1];
                var gap = nextShow.StartTime - currentShow.StartTime;
                if (gap.TotalHours > 0 && gap.TotalHours < 6)
                {
                    currentShow.EndTime = nextShow.StartTime;
                }
                else
                {
                    currentShow.EndTime = currentShow.StartTime.AddHours(1);
                }
            }
            else
            {
                currentShow.EndTime = currentShow.StartTime.AddHours(1);
            }
        }

        return shows;
    }
}
