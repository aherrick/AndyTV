using System.Text.Json;
using AndyTV.Helpers;
using AndyTV.Models;

namespace AndyTV.Services;

public static class ChannelDataService
{
    private const string LastChannelFile = "last_channel.json";
    private const string FavoriteChannelsFile = "favorite_channels.json";

    public static void SaveLastChannel(Channel channel)
    {
        var filePath = PathHelper.GetPath(LastChannelFile);
        var json = JsonSerializer.Serialize(channel);

        File.WriteAllText(filePath, json);
    }

    public static Channel LoadLastChannel()
    {
        try
        {
            var filePath = PathHelper.GetPath(LastChannelFile);
            var json = File.ReadAllText(filePath);

            return JsonSerializer.Deserialize<Channel>(json);
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

            return JsonSerializer.Deserialize<List<Channel>>(json);
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