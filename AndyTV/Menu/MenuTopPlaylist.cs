using AndyTV.Services;

namespace AndyTV.Menu;

public class MenuTopPlaylist(MenuTop top, MenuPlaylist playlist)
{
    public async Task RebuildAll(EventHandler channelClick)
    {
        await PlaylistChannelService.RefreshChannels();

        top.Rebuild(channelClick);
        playlist.Rebuild(channelClick);
    }
}