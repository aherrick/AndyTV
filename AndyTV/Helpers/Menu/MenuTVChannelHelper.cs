using AndyTV.Data.Models;
using AndyTV.Data.Services;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public partial class MenuTVChannelHelper(ContextMenuStrip menu)
{
    private readonly List<ToolStripItem> _added = [];

    private static void AddChannelItem(
        ToolStripMenuItem parent,
        Channel ch,
        EventHandler channelClick,
        string displayText = null
    )
    {
        var item = new ToolStripMenuItem(displayText ?? ch.DisplayName) { Tag = ch };
        item.Click += channelClick;
        parent.DropDownItems.Add(item);
    }

    public async Task RebuildMenu(EventHandler channelClick)
    {
        ClearAddedItems();

        await PlaylistChannelService.RefreshChannels();

        // ----- TOP CHANNELS -----
        var topHeader = MenuHelper.AddHeader(menu, "TOP CHANNELS");
        _added.Add(topHeader);

        BuildTopMenu("US", ChannelService.TopUs(), channelClick, PlaylistChannelService.Channels);
        BuildTopMenu("UK", ChannelService.TopUk(), channelClick, PlaylistChannelService.Channels);
        Build247("24/7", channelClick, PlaylistChannelService.Channels);

        // ----- PLAYLISTS -----
        var playlistsHeader = MenuHelper.AddHeader(menu, "PLAYLISTS");
        _added.Add(playlistsHeader);

        var playlistChannelsMenu = PlaylistChannelService.PlaylistChannels.Where(x =>
            x.Playlist.ShowInMenu
        );

        foreach (var (Playlist, Channels) in playlistChannelsMenu)
        {
            var root = new ToolStripMenuItem(Playlist.Name);

            foreach (var channel in Channels)
            {
                AddChannelItem(root, channel, channelClick);
            }

            if (root.DropDownItems.Count > 0)
            {
                menu.Items.Add(root);
                _added.Add(root);
            }
        }

        Logger.Info("[CHANNELS] Menu rebuilt");
    }

    private void ClearAddedItems()
    {
        foreach (var it in _added)
        {
            int idx = menu.Items.IndexOf(it);
            if (idx >= 0)
            {
                // remove right separator if directly after
                if (idx + 1 < menu.Items.Count && menu.Items[idx + 1] is ToolStripSeparator)
                    menu.Items.RemoveAt(idx + 1);

                // remove left separator if directly before
                if (idx - 1 >= 0 && menu.Items[idx - 1] is ToolStripSeparator)
                    menu.Items.RemoveAt(idx - 1);

                menu.Items.RemoveAt(idx);
            }
        }
        _added.Clear();
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
                AddChannelItem(currentMenu, entry.Channel, channelClick, entry.DisplayText);
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

                AddChannelItem(subMenu, entry.Channel, channelClick, entry.DisplayText);
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
                var matches = new List<Channel>();

                foreach (var ch in channels)
                {
                    foreach (var term in entry.Terms)
                    {
                        var isExact = term.Length <= 2;

                        if (
                            (
                                isExact
                                && string.Equals(
                                    ch.DisplayName,
                                    term,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                            || (
                                !isExact
                                && ch.DisplayName != null
                                && ch.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                            )
                        )
                        {
                            matches.Add(ch);
                            break;
                        }
                    }
                }

                if (matches.Count == 0)
                    continue;

                matches.Sort(
                    (a, b) =>
                        string.Compare(
                            a.DisplayName,
                            b.DisplayName,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

                var parent = new ToolStripMenuItem(entry.Name);
                foreach (var ch in matches)
                {
                    AddChannelItem(parent, ch, channelClick);
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
        var target = (url ?? string.Empty).Trim();

        return PlaylistChannelService.Channels.FirstOrDefault(ch =>
            !string.IsNullOrWhiteSpace(ch.Url)
            && string.Equals(ch.Url.Trim(), target, StringComparison.OrdinalIgnoreCase)
        );
    }
}