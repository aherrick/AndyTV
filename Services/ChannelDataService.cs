using System.Text.Json;
using AndyTV.Helpers;
using AndyTV.Models;

namespace AndyTV.Services;

public static class ChannelDataService
{
    private const string LastChannelFile = "last_channel.txt";
    private const string FavoriteChannelsFile = "favorite_channels.json";

    public static void SaveLastChannel(string name, string url)
    {
        var filePath = PathHelper.GetPath(LastChannelFile);
        File.WriteAllLines(filePath, [name, url]);
    }

    public static (string Name, string Url)? LoadLastChannel()
    {
        try
        {
            var filePath = PathHelper.GetPath(LastChannelFile);
            var lines = File.ReadAllLines(filePath);
            return (lines[0], lines[1]);
        }
        catch
        {
            return null;
        }
    }

    public static List<Channel> LoadFavoriteChannels()
    {
        try
        {
            var filePath = PathHelper.GetPath(FavoriteChannelsFile);
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
        var filePath = PathHelper.GetPath(FavoriteChannelsFile);
        var json = JsonSerializer.Serialize(channels.ToList());
        File.WriteAllText(filePath, json);
    }
}