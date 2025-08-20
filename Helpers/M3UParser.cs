using AndyTV.Models;

namespace AndyTV.Helpers;

public static partial class M3UParser
{
    public static async Task<List<Channel>> Parse(string m3uURL)
    {
        var m3uText = await new HttpClient().GetStringAsync(m3uURL);

        var lines = m3uText.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        var channels = new List<Channel>();

        // Parse to a flat list
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string groupTitle = "Unknown";
            int start = lines[i].IndexOf("group-title=\"", StringComparison.OrdinalIgnoreCase);
            if (start >= 0)
            {
                start += "group-title=\"".Length;
                int end = lines[i].IndexOf('"', start);
                if (end > start)
                {
                    groupTitle = lines[i][start..end];
                }
            }

            string name = lines[i][(lines[i].LastIndexOf(',') + 1)..].Trim();
            string url = (i + 1 < lines.Length) ? lines[i + 1].Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            channels.Add(new Channel(groupTitle, name, url));
        }

        return channels;
    }
}