using System.Text.Json;
using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class RecentChannelService : IRecentChannelService
{
    private const int MaxRecent = 5;
    private const string RecentChannelsFile = "recents.json";

    private readonly IStorageProvider _storageProvider;

    public RecentChannelService(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

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

    private List<Channel> LoadListFromDisk()
    {
        try
        {
            if (!_storageProvider.FileExists(RecentChannelsFile))
                return [];

            var json = _storageProvider.ReadText(RecentChannelsFile);
            return JsonSerializer.Deserialize<List<Channel>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void SaveListToDisk(List<Channel> list)
    {
        var json = JsonSerializer.Serialize(list.Take(MaxRecent));
        _storageProvider.WriteText(RecentChannelsFile, json);
    }
}
