using AndyTV.Data.Models;
using AndyTV.Data.Services;
using AngleSharp;
using AngleSharp.Dom;

namespace AndyTV.Guide.Scraper;

public static class StreamingScraper
{
    public static async Task<List<Show>> GetStreamingGuide()
    {
        // category -> channels
        var top = ChannelService.TopUsGuide();

        var shows = new List<Show>();
        var pstTz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        var channelsWithIds = 0;

        // Tracking for summary
        var zeroResultChannels = new List<(string Category, string Name, string Url)>();
        var noCardBodyChannels = new List<(string Category, string Name, string Url)>();

        foreach (var kvp in top)
        {
            var category = kvp.Key;
            var channels = kvp.Value;

            foreach (var tvChannelFav in channels)
            {
                if (string.IsNullOrWhiteSpace(tvChannelFav.StreamingTVId))
                {
                    continue;
                }

                channelsWithIds++;

                await Task.Delay(15000); // polite delay

                var url = $"https://streamingtvguides.com/Channel/{tvChannelFav.StreamingTVId}";
                Console.WriteLine($"Scraping [{category}] {tvChannelFav.Name} from {url} ...");
                var countBefore = shows.Count;

                try
                {
                    var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
                    var document = await context.OpenAsync(url);

                    var cards = document.QuerySelectorAll(".card-body").ToList();
                    if (cards.Count == 0)
                    {
                        Console.WriteLine(
                            $"[WARN] No .card-body for [{category}] {tvChannelFav.Name}. URL: {url}"
                        );
                        Console.WriteLine("[DEBUG] Dumping first 500 chars of HTML:");
                        Console.WriteLine(
                            document
                                .DocumentElement
                                ?.OuterHtml[..Math.Min(500, document.DocumentElement.OuterHtml.Length)]
                                ?? ""
                        );
                        noCardBodyChannels.Add((category, tvChannelFav.Name, url));
                    }

                    foreach (var showHtml in cards)
                    {
                        var title =
                            showHtml
                                .QuerySelector("h5")
                                ?.TextContent.Replace(
                                    "Playing Now!",
                                    "",
                                    StringComparison.OrdinalIgnoreCase
                                )
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
                        {
                            Console.WriteLine($"[DEBUG] Skipping show (no timeline) → Title: {title}");
                            continue;
                        }

                        var parts = timeLine.Split(" - ", StringSplitOptions.TrimEntries);
                        if (parts.Length != 2)
                        {
                            Console.WriteLine($"[DEBUG] Invalid timeline format: '{timeLine}'");
                            continue;
                        }

                        var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(parts[0]), pstTz);
                        var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse(parts[1]), pstTz);
                        var desc = showHtml.QuerySelector("p.card-text")?.TextContent?.Trim() ?? "";

                        var showDb = new Show
                        {
                            StreamingTVId = tvChannelFav.StreamingTVId,
                            ChannelName = tvChannelFav.Name,
                            Category = category,
                            Subject = title,
                            StartTime = startUtc,
                            EndTime = endUtc,
                            Description = desc,
                        };

                        var exists = shows.FirstOrDefault(p =>
                            p.Subject == showDb.Subject
                            && p.StartTime == showDb.StartTime
                            && p.ChannelName == showDb.ChannelName
                        );

                        if (exists == null && showDb.StartTime > DateTime.UtcNow.AddHours(-6))
                        {
                            shows.Add(showDb);
                        }
                    }

                    var added = shows.Count - countBefore;
                    Console.WriteLine($" → [{category}] {tvChannelFav.Name}: pulled {added} shows");

                    if (added == 0)
                    {
                        zeroResultChannels.Add((category, tvChannelFav.Name, url));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[ERROR] Failed to scrape [{category}] {tvChannelFav.Name} ({url}): {ex}"
                    );
                    // Treat as zero-result for summary clarity
                    zeroResultChannels.Add((category, tvChannelFav.Name, url));
                }
            }
        }

        if (shows.Count == 0)
        {
            Console.WriteLine("[ERROR] RefreshGuide returned ZERO shows. Something is wrong.");
        }

        Console.WriteLine(
            $"TOTAL: {shows.Count} shows across {channelsWithIds} channels (with StreamingTVId)"
        );

        // ----------- CLEAR SUMMARY -----------
        Console.WriteLine();
        Console.WriteLine("==== SUMMARY ====");

        Console.WriteLine("-- Channels with 0 results (no shows added) --");
        if (zeroResultChannels.Count == 0)
        {
            Console.WriteLine("  None");
        }
        else
        {
            foreach (var z in zeroResultChannels.Distinct())
            {
                Console.WriteLine($"  [{z.Category}] {z.Name} → {z.Url}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("-- Channels with no `.card-body` elements --");
        if (noCardBodyChannels.Count == 0)
        {
            Console.WriteLine("  None");
        }
        else
        {
            foreach (var n in noCardBodyChannels.Distinct())
            {
                Console.WriteLine($"  [{n.Category}] {n.Name} → {n.Url}");
            }
        }
        Console.WriteLine("==== END SUMMARY ====");

        return shows;
    }
}
