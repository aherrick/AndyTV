// AndyTV/Menu/MenuRebuilder.cs
using AndyTV.Services;

namespace AndyTV.Menu.Helpers;

public class MenuRebuilder(MenuTop top, MenuPlaylist playlist)
{
    public async Task RebuildAll(EventHandler channelClick)
    {
        await PlaylistChannelService.RefreshChannels();

        top.Rebuild(channelClick);
        playlist.Rebuild(channelClick);
    }
}