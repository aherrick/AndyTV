using System.Net;
using System.Text.Json;
using AndyTV.Helpers;
using AndyTV.Models;
using AndyTV.UI;

namespace AndyTV.Services;

public static class M3UService
{
    private const string M3uSourcesFile = "m3u_sources.json";

    public static async Task<List<Channel>> ParseM3U(string m3uURL)
    {
        var m3uText = await new HttpClient().GetStringAsync(m3uURL);
        var parsed = M3UManager.M3UManager.ParseFromString(m3uText);

        var channels = new List<Channel>(parsed.Channels.Count);

        for (int i = 0; i < parsed.Channels.Count; i++)
        {
            var item = parsed.Channels[i];
            var name = item.TvgName ?? item.Title; // https://github.com/MahdiJamal/M3UManager/issues/26

            // decode only when TvgName exists and has '&'
            if (item.TvgName is { Length: > 0 } && item.TvgName.AsSpan().IndexOf('&') >= 0)
            {
                name = WebUtility.HtmlDecode(item.TvgName);
            }

            channels.Add(
                new Channel
                {
                    Name = name,
                    Url = item.MediaUrl,
                    Group = item.GroupTitle,
                }
            );
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
            using (var dlg = new InputForm(title: "M3U URL", prompt: "Enter M3U URL:"))
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return null; // user cancelled
                }

                url = dlg.Result;
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

        string name;
        using (
            var dlg = new InputForm(
                title: "M3U Name",
                prompt: "Optional name:",
                defaultText: "Default"
            )
        )
        {
            if (dlg.ShowDialog() != DialogResult.OK)
            {
                return null; // cancelled here too
            }

            name = dlg.Result;
        }

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