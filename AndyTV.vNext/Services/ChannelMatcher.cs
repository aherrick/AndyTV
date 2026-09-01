using AndyTV.Data.Models;

namespace AndyTV.vNext;

static class ChannelMatcher
{
    // Name -> every matching channel across all loaded playlists.
    public static Dictionary<string, List<Channel>> BuildLookup(
        IEnumerable<(Playlist Playlist, List<Channel> Channels)> loaded)
    {
        var map = new Dictionary<string, List<Channel>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, channels) in loaded)
        {
            foreach (var c in channels)
            {
                if (!map.TryGetValue(c.Name, out var list))
                {
                    map[c.Name] = list = [];
                }
                if (!list.Any(x => string.Equals(x.Url, c.Url, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(c);
                }
            }
        }
        return map;
    }

    public static List<Channel> Match(ChannelTop top, Dictionary<string, List<Channel>> lookup)
    {
        var result = new List<Channel>();
        foreach (var term in top.Terms)
        {
            if (lookup.TryGetValue(term, out var hit))
            {
                result.AddRange(hit);
            }
        }
        return result;
    }

    public static IEnumerable<IGrouping<string, Channel>> GroupByFirst(IEnumerable<Channel> channels) =>
        channels
            .GroupBy(c => FirstKey(c.DisplayName), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

    private static string FirstKey(string name)
    {
        var ch = name.TrimStart().FirstOrDefault();
        return char.IsLetter(ch) ? char.ToUpperInvariant(ch).ToString() : "#";
    }
}
