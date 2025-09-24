using System.Net;
using System.Text.Json;
using AndyTV.Data.Models;
using AndyTV.Helpers; // PathHelper
using AndyTV.Models;

namespace AndyTV.Services;

public static class PlaylistChannelService
{
    private const string FileName = "playlists.json";

    private static string GetFilePath()
    {
        return PathHelper.GetPath(FileName);
    }

    public static List<Playlist> Load()
    {
        try
        {
            return JsonSerializer.Deserialize<List<Playlist>>(File.ReadAllText(GetFilePath()));
        }
        catch
        {
            return [];
        }
    }

    public static void Save(List<Playlist> items)
    {
        var path = GetFilePath();
        var json = JsonSerializer.Serialize(items);
        File.WriteAllText(path, json);
    }

    public static List<(Playlist Playlist, List<Channel> Channels)> PlaylistChannels
    {
        get;
        private set;
    }

    public static List<Channel> Channels { get; private set; }

    public static async Task RefreshChannels()
    {
        Logger.Info("RefreshChannels: start");

        var source = Load();
        Logger.Info($"Loaded {source.Count} playlists");

        var http = new HttpClient();

        var tasks = source.Select(async p =>
        {
            Logger.Info($"Fetching playlist: {p.Name} ({p.Url})");

            string m3uText;
            try
            {
                m3uText = await http.GetStringAsync(p.Url);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to fetch {p.Url}");
                return (p, new List<Channel>());
            }

            if (string.IsNullOrWhiteSpace(m3uText))
            {
                Logger.Warn($"Playlist {p.Name} returned empty content");
                return (p, new List<Channel>());
            }

            var parsed = M3UManager.M3UManager.ParseFromString(m3uText);
            var channels = new List<Channel>(parsed.Channels.Count);

            for (int i = 0; i < parsed.Channels.Count; i++)
            {
                var item = parsed.Channels[i];
                var name = item.TvgName ?? item.Title;

                if (!string.IsNullOrEmpty(item.TvgName) && item.TvgName.Contains('&'))
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

            Logger.Info($"Playlist {p.Name}: parsed {channels.Count} channels");

            return (p, channels);
        });

        PlaylistChannels = [.. await Task.WhenAll(tasks)];

        Channels =
        [
            .. PlaylistChannels
                .SelectMany(x => x.Channels)
                .GroupBy(c => c.Url, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First()),
        ];

        Logger.Info($"RefreshChannels: total unique channels = {Channels.Count}");
        Logger.Info("RefreshChannels: done");
    }
}