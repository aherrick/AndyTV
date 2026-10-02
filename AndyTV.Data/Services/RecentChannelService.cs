using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public class RecentChannelService(IStorageProvider storage) : IRecentChannelService
{
    private const int MaxRecent = 5;
    private const string RecentChannelsFile = "recents.json";

    public void AddOrPromote(Channel channel)
    {
        if (string.IsNullOrWhiteSpace(channel?.Url))
        {
            return;
        }

        var list = GetRecentChannels();
        list.RemoveAll(x => string.Equals(x?.Url, channel.Url, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, channel);
        storage.WriteJson(RecentChannelsFile, list.Take(MaxRecent));
    }

    public Channel GetPrevious()
    {
        var recents = GetRecentChannels();
        return recents.Count > 1 ? recents[1] : null;
    }

    public Channel GetRelative(string currentUrl, int direction)
    {
        var recents = GetRecentChannels();
        if (recents.Count == 0)
        {
            return null;
        }

        var currentIndex = recents.FindIndex(c =>
            string.Equals(c.Url, currentUrl, StringComparison.OrdinalIgnoreCase));

        if (currentIndex < 0)
        {
            return direction >= 0 ? recents[0] : recents[^1];
        }

        var nextIndex = (currentIndex + direction + recents.Count) % recents.Count;
        return recents[nextIndex];
    }

    public List<Channel> GetRecentChannels() =>
        [.. (storage.ReadJson<List<Channel>>(RecentChannelsFile) ?? []).Take(MaxRecent)];
}
