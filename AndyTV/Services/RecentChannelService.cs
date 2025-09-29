using System.Text.Json;
using AndyTV.Data.Models;
using AndyTV.Helpers;

namespace AndyTV.Services;

public static class RecentChannelService
{
    private const int MaxRecent = 5;
    private const string RecentChannelsFile = "recents.json";

    public static void AddOrPromote(Channel channel)
    {
        if (channel == null || string.IsNullOrWhiteSpace(channel.Url))
            return;

        var list = LoadListFromDisk();
        list.RemoveAll(x => string.Equals(x?.Url, channel.Url, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, channel);

        if (list.Count > MaxRecent)
        {
            list = [.. list.Take(MaxRecent)];
        }

        SaveListToDisk(list);
    }

    public static Channel GetPrevious()
    {
        var list = LoadListFromDisk().Take(MaxRecent).ToList();
        return list.ElementAtOrDefault(1);
    }

    public static List<Channel> GetRecentChannels()
    {
        return [.. LoadListFromDisk().Take(MaxRecent)];
    }

    private static List<Channel> LoadListFromDisk()
    {
        try
        {
            var fileName = PathHelper.GetPath(RecentChannelsFile);
            var json = File.ReadAllText(fileName);
            return JsonSerializer.Deserialize<List<Channel>>(json);
        }
        catch
        {
            return [];
        }
    }

    private static void SaveListToDisk(List<Channel> list)
    {
        var fileName = PathHelper.GetPath(RecentChannelsFile);
        File.WriteAllText(fileName, JsonSerializer.Serialize(list.Take(MaxRecent)));
    }
}