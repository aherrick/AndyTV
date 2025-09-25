using AndyTV.Data.Models;
using AndyTV.Data.Services;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public partial class MenuTVChannelHelper(ContextMenuStrip menu)
{
    private readonly List<ToolStripItem> _added = [];

    public async Task RebuildMenu(EventHandler channelClick)
    {
        foreach (var it in _added)
        {
            if (menu.Items.Contains(it))
            {
                menu.Items.Remove(it);
            }
        }
        _added.Clear();

        await PlaylistChannelService.RefreshChannels();

        // ----- TOP CHANNELS -----
        var (_, topAll) = MenuHelper.AddHeader(menu, "TOP CHANNELS");
        _added.AddRange(topAll);

        BuildTopMenu("US", ChannelService.TopUs(), channelClick, PlaylistChannelService.Channels);
        BuildTopMenu("UK", ChannelService.TopUk(), channelClick, PlaylistChannelService.Channels);

        // ----- 24/7 -----
        Build247("24/7", channelClick, PlaylistChannelService.Channels);

        // ----- PLAYLISTS -----
        BuildPlaylistSection(channelClick);

        Logger.Info("[CHANNELS] Menu rebuilt");
    }

    private void BuildPlaylistSection(EventHandler channelClick)
    {
        var (_, playlistsAll) = MenuHelper.AddHeader(menu, "PLAYLISTS");
        _added.AddRange(playlistsAll);

        var playlistChannelsMenu = PlaylistChannelService.PlaylistChannels.Where(x =>
            x.Playlist.ShowInMenu
        );

        foreach (var (Playlist, Channels) in playlistChannelsMenu)
        {
            var root = new ToolStripMenuItem(Playlist.Name);

            foreach (var channel in Channels)
            {
                MenuHelper.AddChildChannelItem(root, channel, channelClick);
            }

            if (root.DropDownItems.Count > 0)
            {
                menu.Items.Add(root);
                _added.Add(root);
            }
        }
    }

    public void Build247(string rootTitle, EventHandler channelClick, List<Channel> channels)
    {
        var root = new ToolStripMenuItem(rootTitle);
        var entries = ChannelService.Get247Entries(rootTitle, channels);

        string currentBucket = null;
        ToolStripMenuItem currentMenu = null;

        foreach (var entry in entries)
        {
            if (!string.Equals(entry.Bucket, currentBucket, StringComparison.Ordinal))
            {
                if (currentMenu != null && currentMenu.DropDownItems.Count > 0)
                {
                    root.DropDownItems.Add(currentMenu);
                }

                currentBucket = entry.Bucket;
                currentMenu = new ToolStripMenuItem(currentBucket);
            }

            if (entry.GroupBase == null)
            {
                MenuHelper.AddChildChannelItem(
                    currentMenu,
                    entry.Channel,
                    channelClick,
                    entry.DisplayText
                );
            }
            else
            {
                var subMenu =
                    currentMenu
                        .DropDownItems.OfType<ToolStripMenuItem>()
                        .FirstOrDefault(m => m.Text == entry.GroupBase)
                    ?? new ToolStripMenuItem(entry.GroupBase);

                if (!currentMenu.DropDownItems.Contains(subMenu))
                {
                    currentMenu.DropDownItems.Add(subMenu);
                }

                MenuHelper.AddChildChannelItem(
                    subMenu,
                    entry.Channel,
                    channelClick,
                    entry.DisplayText
                );
            }
        }

        if (currentMenu != null && currentMenu.DropDownItems.Count > 0)
        {
            root.DropDownItems.Add(currentMenu);
        }

        if (root.DropDownItems.Count > 0)
        {
            menu.Items.Add(root);
            _added.Add(root);
        }
    }

    private void BuildTopMenu(
        string rootTitle,
        Dictionary<string, List<ChannelTop>> categories,
        EventHandler channelClick,
        List<Channel> channels
    )
    {
        var rootItem = new ToolStripMenuItem(rootTitle);

        foreach (
            var (catName, entries) in categories.OrderBy(
                k => k.Key,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            var catItem = new ToolStripMenuItem(catName);

            foreach (var entry in entries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
            {
                var matches = channels
                    .Where(ch =>
                        entry.Terms.Any(term =>
                            ch.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    .OrderBy(ch => ch.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (matches.Count == 0)
                {
                    continue;
                }

                var parent = new ToolStripMenuItem(entry.Name);
                foreach (var ch in matches)
                {
                    MenuHelper.AddChildChannelItem(parent, ch, channelClick);
                }

                catItem.DropDownItems.Add(parent);
            }

            if (catItem.DropDownItems.Count > 0)
            {
                rootItem.DropDownItems.Add(catItem);
            }
        }

        if (rootItem.DropDownItems.Count > 0)
        {
            menu.Items.Add(rootItem);
            _added.Add(rootItem);
        }
    }

    public static Channel ChannelByUrl(string url)
    {
        return PlaylistChannelService.Channels.FirstOrDefault(ch =>
            !string.IsNullOrWhiteSpace(ch.Url)
            && string.Equals(ch.Url.Trim(), url.Trim(), StringComparison.OrdinalIgnoreCase)
        );
    }
}