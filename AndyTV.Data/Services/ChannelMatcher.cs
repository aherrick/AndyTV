using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

// Pure matching/grouping so every app builds identical channel menus from a plain node tree.
public static class ChannelMatcher
{
    // Substring match on DisplayName across all Terms, honoring ExcludeTerms; ordered by name.
    public static List<Channel> MatchTop(ChannelTop entry, IReadOnlyList<Channel> channels)
    {
        var matches = new List<Channel>();
        foreach (var ch in channels)
        {
            var name = ch.DisplayName;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }
            if (!ContainsAny(name, entry.Terms))
            {
                continue;
            }
            if (entry.ExcludeTerms is { } excludes && ContainsAny(name, excludes))
            {
                continue;
            }
            matches.Add(ch);
        }
        matches.Sort(
            (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return matches;
    }

    public static MenuNode BuildTopRegion(
        string region,
        Dictionary<string, List<ChannelTop>> categories,
        IReadOnlyList<Channel> channels)
    {
        var regionNode = new MenuNode { Text = region, Children = [] };
        foreach (
            var (category, entries) in categories.OrderBy(
                c => c.Key,
                StringComparer.OrdinalIgnoreCase))
        {
            var categoryNode = new MenuNode { Text = category, Children = [] };
            foreach (var entry in entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
            {
                var matches = MatchTop(entry, channels);
                if (matches.Count == 0)
                {
                    continue;
                }
                var entryNode = new MenuNode { Text = entry.Name, Children = [] };
                foreach (var ch in matches)
                {
                    entryNode.Children.Add(Leaf(ch, ch.DisplayName));
                }
                categoryNode.Children.Add(entryNode);
            }
            if (categoryNode.Children.Count > 0)
            {
                regionNode.Children.Add(categoryNode);
            }
        }
        return regionNode;
    }

    public static List<MenuNode> BuildPlaylistNodes(Playlist playlist, IReadOnlyList<Channel> channels)
    {
        if (!playlist.GroupByFirstChar)
        {
            var flat = new List<MenuNode>(channels.Count);
            foreach (var ch in channels)
            {
                flat.Add(Leaf(ch, ch.DisplayName));
            }
            return flat;
        }

        // Extra title level only when a name transform strips episode info from RawName.
        var episodic =
            !string.IsNullOrWhiteSpace(playlist.NameFind) && playlist.NameReplace is not null;

        var letters = new List<MenuNode>();
        var groups = channels
            .GroupBy(ch => FirstCharKey(ch.DisplayName), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var letterGroup in groups)
        {
            var letterNode = new MenuNode { Text = letterGroup.Key, Children = [] };
            if (episodic)
            {
                var titles = letterGroup
                    .GroupBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
                foreach (var titleGroup in titles)
                {
                    var items = titleGroup
                        .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (items.Count == 1)
                    {
                        letterNode.Children.Add(Leaf(items[0], items[0].RawName));
                    }
                    else
                    {
                        var titleNode = new MenuNode { Text = titleGroup.Key, Children = [] };
                        foreach (var ch in items)
                        {
                            titleNode.Children.Add(Leaf(ch, ch.RawName));
                        }
                        letterNode.Children.Add(titleNode);
                    }
                }
            }
            else
            {
                foreach (
                    var ch in letterGroup.OrderBy(
                        c => c.DisplayName,
                        StringComparer.OrdinalIgnoreCase))
                {
                    letterNode.Children.Add(Leaf(ch, ch.DisplayName));
                }
            }
            if (letterNode.Children.Count > 0)
            {
                letters.Add(letterNode);
            }
        }
        return letters;
    }

    private static MenuNode Leaf(Channel channel, string text) =>
        new() { Text = text, Channel = channel };

    private static bool ContainsAny(string name, IEnumerable<string> terms)
    {
        foreach (var term in terms)
        {
            if (name.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string FirstCharKey(string displayName)
    {
        var trimmed = displayName?.TrimStart();
        var ch = string.IsNullOrEmpty(trimmed) ? '#' : char.ToUpperInvariant(trimmed[0]);
        return char.IsLetterOrDigit(ch) ? ch.ToString() : "#";
    }
}
