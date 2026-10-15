using AndyTV.Data.Models;
using AndyTV.Data.Services;

namespace AndyTV.Tests;

// Each Lists/*.txt holds "Group = count  # note" expectations, a "---" line, then one channel name per line.
[TestClass]
public sealed class SmartGroupTests
{
    private static readonly string ListsDir = Path.Combine(AppContext.BaseDirectory, "Lists");

    public static IEnumerable<object[]> Lists =>
        Directory.GetFiles(ListsDir, "*.txt").Select(f => new object[] { Path.GetFileName(f) });

    [TestMethod]
    [DynamicData(nameof(Lists))]
    public void GroupCountsMatch(string file)
    {
        var lines = File.ReadAllLines(Path.Combine(ListsDir, file));
        var split = Array.IndexOf(lines, "---");
        Assert.AreNotEqual(-1, split, $"{file} is missing the '---' line.");

        var expected = lines[..split]
            .Select(l => l.Split('#')[0])
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split('=', 2))
            .Select(p => (Group: p[0].Trim(), Count: int.Parse(p[1])));

        var channels = lines[(split + 1)..]
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select((name, i) => new Channel { Name = name, RawName = name, Url = $"u{i}" })
            .ToList();

        var actual = ChannelMatcher
            .BuildPlaylistNodes(new Playlist { SmartGroup = true }, channels)
            .Where(n => !n.IsSeparator)
            .ToDictionary(n => n.Text, n => n.Children.Count);

        var summary = string.Join(", ", actual.Select(kv => $"{kv.Key} = {kv.Value}"));
        foreach (var (group, count) in expected)
        {
            Assert.AreEqual(count, actual.GetValueOrDefault(group), $"'{group}' in {file}. Actual: {summary}");
        }
    }
}
