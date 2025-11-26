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
        // freezes the ui without task run
        await Task.Run(async () =>
        {
            Logger.Info("Refreshing channels...");

            var source = Load();
            Logger.Info($"Playlists: {source.Count}");

            var http = new HttpClient();

            var tasks = source.Select(async p =>
            {
                try
                {
                    var m3uText = await http.GetStringAsync(p.Url);
                    if (string.IsNullOrWhiteSpace(m3uText))
                    {
                        Logger.Warn($"Empty playlist: {p.Name}");
                        return (p, new List<Channel>());
                    }

                    var parsed = M3UManager.M3UManager.ParseFromString(m3uText);
                    var channels = new List<Channel>(parsed.Channels.Count);

                    foreach (var item in parsed.Channels)
                    {
                        var url = item.MediaUrl;

                        // Apply regex transformation if pattern is provided
                        if (!string.IsNullOrWhiteSpace(p.UrlRegex))
                        {
                            try
                            {
                                // Parse s/pattern/replacement/ syntax, handling escaped forward slashes
                                var match = System.Text.RegularExpressions.Regex.Match(
                                    p.UrlRegex,
                                    @"^s/((?:[^\\/]|\\.)*)/((?:[^\\/]|\\.)*)/?"
                                );
                                if (match.Success)
                                {
                                    // Unescape the captured values (\/ becomes /)
                                    var pattern = match.Groups[1].Value.Replace("\\/", "/");
                                    var replacement = match.Groups[2].Value.Replace("\\/", "/");
                                    url = System.Text.RegularExpressions.Regex.Replace(
                                        url,
                                        pattern,
                                        replacement
                                    );
                                }
                                else
                                {
                                    Logger.Warn(
                                        $"Invalid regex format for {p.Name}. Expected: s/pattern/replacement/"
                                    );
                                }
                            }
                            catch (Exception regexEx)
                            {
                                Logger.Warn(
                                    $"Regex failed for {p.Name}: {regexEx.Message}"
                                );
                            }
                        }

                        channels.Add(
                            new Channel
                            {
                                Name = item.Title,
                                Url = url,
                                Group = item.GroupTitle,
                            }
                        );
                    }

                    Logger.Info($"Playlist {p.Name}: {channels.Count} channels");
                    return (p, channels);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed: {p.Name} ({p.Url})");
                    return (p, new List<Channel>());
                }
            });

            PlaylistChannels = [.. await Task.WhenAll(tasks)];

            Channels =
            [
                .. PlaylistChannels
                    .SelectMany(x => x.Channels)
                    .GroupBy(c => c.Url, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First()),
            ];

            Logger.Info($"Unique channels: {Channels.Count}");
        });
    }
}
