using System.Text.Json;
using AndyTV.Helpers;
using AndyTV.Models;
using m3uParser;

namespace AndyTV.Services;

public static class M3UService
{
    private const string M3uSourcesFile = "m3u_sources.json";

    public static async Task<List<Channel>> ParseM3U(string m3uURL)
    {
        var m3uText = await new HttpClient().GetStringAsync(m3uURL);

        var contentM3u = M3U.Parse(m3uText);

        var channels = new List<Channel>();

        foreach (var item in contentM3u.Medias)
        {
            channels.Add(new Channel() { Name = item.Title.RawTitle, Url = item.MediaFile });
        }

        return channels;
    }

    public record M3USource(string Name, string Url);

    public static List<M3USource> LoadSources()
    {
        var fileName = PathHelper.GetPath(M3uSourcesFile);
        if (!File.Exists(fileName))
            return [];

        var json = File.ReadAllText(fileName);
        return JsonSerializer.Deserialize<List<M3USource>>(json) ?? [];
    }

    public static void SaveSources(List<M3USource> sources)
    {
        var fileName = PathHelper.GetPath(M3uSourcesFile);
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
            {
                return null; // user cancelled
            }

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