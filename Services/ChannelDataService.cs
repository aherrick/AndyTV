using System.Text.Json;
using AndyTV.Helpers;
using AndyTV.Models;

namespace AndyTV.Services;

public static class ChannelDataService
{
    public static void SaveLastChannel(string name, string url)
    {
        var filePath = PathHelper.GetPath("last_channel.txt");
        File.WriteAllLines(filePath, [name, url]);
    }

    public static (string Name, string Url)? LoadLastChannel()
    {
        var filePath = PathHelper.GetPath("last_channel.txt");
        if (!File.Exists(filePath))
        {
            return null;
        }

        var lines = File.ReadAllLines(filePath);
        return (lines[0], lines[1]);
    }

    public static List<Channel> LoadFavoriteChannels()
    {
        var filePath = PathHelper.GetPath("favorite_channels.json");
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<Channel>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void SaveFavoriteChannels(IEnumerable<Channel> channels)
    {
        var filePath = PathHelper.GetPath("favorite_channels.json");
        var json = JsonSerializer.Serialize(channels.ToList());
        File.WriteAllText(filePath, json);
    }
}