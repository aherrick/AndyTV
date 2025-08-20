using System.Text.Json;
using Microsoft.VisualBasic;

namespace AndyTV.Helpers;

public static class M3USourceStore
{
    private static readonly string FileName = PathHelper.GetPath("m3u_sources.json");

    public record M3USource(string Name, string Url);

    public static List<M3USource> Load()
    {
        if (!File.Exists(FileName))
            return [];

        var json = File.ReadAllText(FileName);
        return JsonSerializer.Deserialize<List<M3USource>>(json);
    }

    public static void Save(List<M3USource> sources)
    {
        var json = JsonSerializer.Serialize(sources);
        File.WriteAllText(FileName, json);
    }

    public static M3USource GetOrPromptFirst()
    {
        var sources = Load();

        // support just one source for now, but down the road we can support multiple
        if (sources.Count == 0)
        {
            string url;
            while (true)
            {
                url = Interaction.InputBox("Enter M3U URL:", "M3U URL", "");
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

            var name = Interaction.InputBox("Optional name:", "M3U Name", "Default");
            var src = new M3USource(string.IsNullOrWhiteSpace(name) ? "Default" : name, url.Trim());
            sources.Add(src);
            Save(sources);
        }

        return sources[0];
    }
}