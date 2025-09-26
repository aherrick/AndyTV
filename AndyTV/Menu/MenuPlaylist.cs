// AndyTV/Menu/MenuPlaylists.cs
using AndyTV.Services;

namespace AndyTV.Menu;

public sealed class MenuPlaylist(ContextMenuStrip menu)
{
    private readonly List<ToolStripItem> _added = [];

    public void Rebuild(EventHandler channelClick)
    {
        // remove previously added items for a clean rebuild
        foreach (var it in _added)
        {
            if (menu.Items.Contains(it))
            {
                menu.Items.Remove(it);
            }
        }
        _added.Clear();

        var (_, headerItems) = MenuHelper.AddHeader(menu, "PLAYLISTS");
        _added.AddRange(headerItems);

        var playlistChannelsMenu = PlaylistChannelService.PlaylistChannels.Where(x =>
            x.Playlist.ShowInMenu
        );

        foreach (var (Playlist, Channels) in playlistChannelsMenu)
        {
            var root = new ToolStripMenuItem(Playlist.Name);

            foreach (var ch in Channels)
            {
                MenuHelper.AddChildChannelItem(root, ch, channelClick);
            }

            if (root.DropDownItems.Count > 0)
            {
                menu.Items.Add(root);
                _added.Add(root);
            }
        }
    }
}