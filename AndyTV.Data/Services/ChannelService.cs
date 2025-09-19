using System.Text.RegularExpressions;
using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public partial class ChannelService
{
    // Pure/static version for tests
    public static List<MenuEntry> Get247Entries(string rootTitle, IEnumerable<Channel> channels)
    {
        // ---- Local helpers ----
        static string CleanBaseName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var text = name;
            text = TagsRegex().Replace(text, ""); // remove [VIP], [HD], etc.
            text = TwoFourSevenRegex().Replace(text, ""); // remove 24/7
            text = SeasonShortRegex().Replace(text, ""); // strip "S01" etc. from base
            text = SeasonLongRegex().Replace(text, ""); // strip "Season 1" etc. from base
            text = NormalizeSpaceRegex().Replace(text, " ").Trim();
            return text;
        }

        static (string Base, string Season) ExtractBaseAndSeason(string originalName)
        {
            var baseName = CleanBaseName(originalName);

            // Prefer short season first (S01); if none, look for "Season 1"
            var seasonMatch = SeasonShortRegex().Match(originalName);
            if (!seasonMatch.Success)
            {
                seasonMatch = SeasonLongRegex().Match(originalName);
            }

            return (baseName, seasonMatch.Success ? seasonMatch.Value : null);
        }

        // ---- Select candidates: must contain the rootTitle (e.g., "24/7") and not match the (AA) marker ----
        var candidates = channels
            .Where(ch =>
            {
                if (ch is null || string.IsNullOrWhiteSpace(ch.DisplayName))
                {
                    return false;
                }

                // Match "24/7 ..." (or whatever rootTitle is) and filter out "(AA)"-style two-letter markers
                if (ch.DisplayName.IndexOf(rootTitle, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }

                if (MatchTwoParens().IsMatch(ch.DisplayName))
                {
                    return false;
                }

                return true;
            })
            .Select(ch => new { Channel = ch, Info = ExtractBaseAndSeason(ch.DisplayName) });

        // ---- Group by cleaned base name; sort groups & items deterministically ----
        var grouped = candidates
            .GroupBy(x => x.Info.Base, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                BaseName = g.Key,
                Items = g.OrderBy(
                        x => x.Info.Season ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ThenBy(x => x.Channel.DisplayName, StringComparer.OrdinalIgnoreCase),
            });

        // ---- Build entries ----
        var entries = new List<MenuEntry>();

        foreach (var group in grouped)
        {
            if (string.IsNullOrWhiteSpace(group.BaseName))
            {
                continue; // skip empties
            }

            char first = group.BaseName[0];
            string bucket;

            if (char.IsDigit(first))
            {
                bucket = "1-9";
            }
            else if (char.IsLetter(first))
            {
                bucket = char.ToUpperInvariant(first).ToString();
            }
            else
            {
                bucket = null;
            }

            if (bucket is null)
            {
                continue; // skip non-letter, non-digit groups
            }

            bool hasMultiple = group.Items.Skip(1).Any();

            foreach (var item in group.Items)
            {
                // Display text is the cleaned base plus season (if present)
                string display = group.BaseName;
                if (!string.IsNullOrEmpty(item.Info.Season))
                {
                    display = $"{display} {item.Info.Season}";
                }

                entries.Add(
                    new MenuEntry
                    {
                        Bucket = bucket,
                        GroupBase = hasMultiple ? group.BaseName : null, // null for singletons
                        DisplayText = display,
                        Channel = item.Channel,
                    }
                );
            }
        }

        // ---- Final ordering: Bucket -> (GroupBase or DisplayText) ----
        return
        [
            .. entries
                .OrderBy(e => e.Bucket, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.GroupBase ?? e.DisplayText, StringComparer.OrdinalIgnoreCase),
        ];
    }

    // ----------------- Regex helpers -----------------
    [GeneratedRegex(@"\([A-Za-z]{2}\)", RegexOptions.Compiled)]
    private static partial Regex MatchTwoParens();

    [GeneratedRegex(@"24\s*/\s*7", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TwoFourSevenRegex();

    [GeneratedRegex(@"\[[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TagsRegex();

    [GeneratedRegex(@"(?<!\w)S\d{2}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasonShortRegex();

    [GeneratedRegex(@"Season\s*\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasonLongRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex NormalizeSpaceRegex();
}