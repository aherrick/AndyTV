using Mgr = M3UManager.M3UManager;

namespace AndyTV.vNext;

static class PlaylistService
{
    public static async Task<List<ChannelRef>> Load(PlaylistRef playlist)
    {
        var m3u = File.Exists(playlist.Source)
            ? Mgr.ParseFromFile(playlist.Source)
            : await Mgr.ParseFromUrlAsync(playlist.Source);

        return m3u.Channels
            .Where(c => !string.IsNullOrWhiteSpace(c.MediaUrl))
            .Select(c => new ChannelRef { Name = c.Title ?? c.TvgName ?? c.MediaUrl, Url = c.MediaUrl })
            .ToList();
    }
}
