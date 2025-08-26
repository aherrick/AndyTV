using System.Text.Json;
using AndyTV.Helpers;
using AndyTV.Models;

namespace AndyTV.Services;

public static class M3UService
{
    public static async Task<List<Channel>> ParseM3U(string m3uURL)
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

            channels.Add(
                new Channel()
                {
                    Group = groupTitle,
                    Name = name,
                    Url = url,
                }
            );
        }

        return channels;
    }

    public record M3USource(string Name, string Url);

    public static List<M3USource> LoadSources()
    {
        var fileName = PathHelper.GetPath("m3u_sources.json");
        if (!File.Exists(fileName))
            return [];

        var json = File.ReadAllText(fileName);
        return JsonSerializer.Deserialize<List<M3USource>>(json) ?? [];
    }

    public static void SaveSources(List<M3USource> sources)
    {
        var fileName = PathHelper.GetPath("m3u_sources.json");
        var json = JsonSerializer.Serialize(sources);
        File.WriteAllText(fileName, json);
    }

    public static M3USource TryGetFirstSource()
    {
        return LoadSources().FirstOrDefault();
    }

    public static M3USource PromptNewSource()
    {
        string url;
        while (true)
        {
            url = Microsoft.VisualBasic.Interaction.InputBox("Enter M3U URL:", "M3U URL", "");
            if (string.IsNullOrWhiteSpace(url))
                return null; // user cancelled

            if (
                Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            )
            {
                break; // valid input
            }

            MessageBox.Show("Please enter a valid HTTP/HTTPS URL or Cancel to quit.");
        }

        var name = Microsoft.VisualBasic.Interaction.InputBox(
            "Optional name:",
            "M3U Name",
            "Default"
        );
        var src = new M3USource(
            string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim(),
            url.Trim()
        );

        var sources = LoadSources();
        sources.Add(src);
        SaveSources(sources);

        return src;
    }
}