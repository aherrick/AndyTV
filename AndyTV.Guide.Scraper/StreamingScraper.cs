using System.Text.Json;
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
        var channelsWithIds = 0;

        // Tracking for summary
        var zeroResultChannels = new List<(string Category, string Name, string Url)>();

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

                    // The page embeds the full schedule as JSON-LD (CollectionPage -> BroadcastEvent items).
                    var json = document
                        .QuerySelectorAll("script[type='application/ld+json']")
                        .Select(script => script.TextContent)
                        .FirstOrDefault(text => text.Contains("\"CollectionPage\""));

                    if (json is null)
                    {
                        Console.WriteLine(
                            $"[WARN] No schedule JSON-LD for [{category}] {tvChannelFav.Name}. URL: {url}"
                        );
                        zeroResultChannels.Add((category, tvChannelFav.Name, url));
                        continue;
                    }

                    foreach (var show in ParseSchedule(json, category, tvChannelFav))
                    {
                        var exists = shows.Any(p =>
                            p.Subject == show.Subject
                            && p.StartTime == show.StartTime
                            && p.ChannelName == show.ChannelName
                        );

                        if (!exists && show.StartTime > DateTime.UtcNow.AddHours(-6))
                        {
                            shows.Add(show);
                        }
                    }

                    var added = shows.Count - countBefore;
                    Console.WriteLine($" -> [{category}] {tvChannelFav.Name}: pulled {added} shows");

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
                Console.WriteLine($"  [{z.Category}] {z.Name} -> {z.Url}");
            }
        }
        Console.WriteLine("==== END SUMMARY ====");

        return shows;
    }

    // Maps CollectionPage JSON-LD BroadcastEvent items to Show (startDate/endDate are UTC).
    private static List<Show> ParseSchedule(string json, string category, ChannelTop channel)
    {
        var shows = new List<Show>();

        using var doc = JsonDocument.Parse(json);
        if (
            !doc.RootElement.TryGetProperty("mainEntity", out var mainEntity)
            || !mainEntity.TryGetProperty("itemListElement", out var list)
            || list.ValueKind != JsonValueKind.Array
        )
        {
            return shows;
        }

        foreach (var element in list.EnumerateArray())
        {
            if (!element.TryGetProperty("item", out var item))
            {
                continue;
            }

            var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (
                !item.TryGetProperty("startDate", out var startEl)
                || !item.TryGetProperty("endDate", out var endEl)
                || !startEl.TryGetDateTimeOffset(out var start)
                || !endEl.TryGetDateTimeOffset(out var end)
            )
            {
                continue;
            }

            shows.Add(
                new Show
                {
                    StreamingTVId = channel.StreamingTVId,
                    ChannelName = channel.Name,
                    Category = category,
                    Subject = name,
                    Description = item.TryGetProperty("description", out var descEl)
                        ? descEl.GetString() ?? ""
                        : "",
                    StartTime = start.UtcDateTime,
                    EndTime = end.UtcDateTime,
                }
            );
        }

        return shows;
    }
}
