using System.Text.Json;
using AndyTV.Helpers;
using AndyTV.Models;

namespace AndyTV.Services;

public static class ChannelDataService
{
    private const string LastChannelFile = "last_channel.json";
    public const string FavoriteChannelsFile = "favorite_channels.json";

    // --- Last Channel ---
    public static void SaveLastChannel(Channel channel)
    {
        string path = PathHelper.GetPath(LastChannelFile);
        string json = JsonSerializer.Serialize(channel);
        File.WriteAllText(path, json);
    }

    public static Channel LoadLastChannel()
    {
        try
        {
            string path = PathHelper.GetPath(LastChannelFile);
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Channel>(json);
        }
        catch
        {
            return null;
        }
    }

    // --- Favorites (app data path) ---
    public static List<Channel> LoadFavoriteChannels()
    {
        try
        {
            string path = PathHelper.GetPath(FavoriteChannelsFile);
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<Channel>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void SaveFavoriteChannels(IEnumerable<Channel> channels)
    {
        string path = PathHelper.GetPath(FavoriteChannelsFile);
        string json = JsonSerializer.Serialize(channels.ToList());
        File.WriteAllText(path, json);
    }

    // --- Import/Export (arbitrary file paths for dialogs) ---
    public static List<Channel> ImportFavoriteChannels(string filePath)
    {
        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<Channel>>(json);
    }

    public static void ExportFavoriteChannels(IEnumerable<Channel> channels, string filePath)
    {
        string json = JsonSerializer.Serialize(channels.ToList());
        File.WriteAllText(filePath, json);
    }
}
