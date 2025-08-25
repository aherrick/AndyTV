using System.Text.Json;
using AndyTV.Helpers;
using AndyTV.Models;

namespace AndyTV.Services;

public class RecentChannelsService
{
    private readonly int MaxRecent = 5;

    public void AddOrPromote(Channel channel)
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

    public Channel GetPrevious()
    {
        var list = LoadListFromDisk().Take(MaxRecent).ToList();
        return list.ElementAtOrDefault(1);
    }

    public List<Channel> GetRecentChannels()
    {
        return [.. LoadListFromDisk().Take(MaxRecent)];
    }

    private static List<Channel> LoadListFromDisk()
    {
        var fileName = PathHelper.GetPath("recents.json");
        if (!File.Exists(fileName))
        {
            File.WriteAllText(fileName, "[]");
            return [];
        }

        var json = File.ReadAllText(fileName);
        return JsonSerializer.Deserialize<List<Channel>>(json) ?? [];
    }

    private void SaveListToDisk(List<Channel> list)
    {
        var fileName = PathHelper.GetPath("recents.json");
        var trimmed = list.Take(MaxRecent).ToList();
        var json = JsonSerializer.Serialize(trimmed);
        File.WriteAllText(fileName, json);
    }
}