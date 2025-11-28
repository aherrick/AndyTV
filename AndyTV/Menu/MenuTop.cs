using AndyTV.Data.Models;
using AndyTV.Data.Services;
using AndyTV.Helpers;

namespace AndyTV.Menu;

public partial class MenuTop(ContextMenuStrip menu, SynchronizationContext ui, IPlaylistService playlistService)
{
    private readonly SynchronizationContext _ui = ui;
    private readonly List<ToolStripItem> _added = [];

    public void Rebuild(EventHandler channelClick)
    {
        _ui.Post(
            _ =>
            {
                foreach (var it in _added)
                {
                    if (menu.Items.Contains(it))
                    {
                        menu.Items.Remove(it);
                    }
                }
                _added.Clear();

                // ----- CHANNELS -----
                var (_, topAll) = MenuHelper.AddHeader(menu, "CHANNELS");
                _added.AddRange(topAll);

                // Add playlists first (before US/UK)
                BuildPlaylistMenu(channelClick);

                BuildTopMenu(
                    "US",
                    ChannelService.TopUs(),
                    channelClick,
                    playlistService.Channels
                );
                BuildTopMenu(
                    "UK",
                    ChannelService.TopUk(),
                    channelClick,
                    playlistService.Channels
                );

                // ----- 24/7 -----
                Build247("24/7", channelClick, playlistService.Channels);

                Logger.Info("[CHANNELS] Menu rebuilt (Top + 24/7)");
            },
            null
        );
    }

    public void Build247(string rootTitle, EventHandler channelClick, List<Channel> channels)
    {
        var root = new ToolStripMenuItem(rootTitle);
        var entries = ChannelService.Get247Entries(channels);

        string currentBucket = null;
        ToolStripMenuItem currentMenu = null;

        foreach (var entry in entries)
        {
            if (!string.Equals(entry.Bucket, currentBucket, StringComparison.Ordinal))
            {
                if (currentMenu?.DropDownItems.Count > 0)
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

        if (currentMenu?.DropDownItems.Count > 0)
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
                        && entry.ExcludeTerms?.Any(ex =>
                            ch.DisplayName.Contains(ex, StringComparison.OrdinalIgnoreCase)
                        ) != true
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

    private void BuildPlaylistMenu(EventHandler channelClick)
    {
        var playlistChannelsMenu = playlistService.PlaylistChannels.Where(x =>
            x.Playlist.ShowInMenu
        );

        foreach (var (Playlist, Channels) in playlistChannelsMenu)
        {
            var root = new ToolStripMenuItem(Playlist.Name);

            if (Playlist.GroupByFirstChar)
            {
                var grouped = Channels
                    .GroupBy(ch => char.ToUpperInvariant(ch.DisplayName.FirstOrDefault()))
                    .OrderBy(g => g.Key);

                foreach (var group in grouped)
                {
                    var groupKey = group.Key;
                    if (!char.IsLetterOrDigit(groupKey))
                        groupKey = '#';

                    var subMenu = new ToolStripMenuItem(groupKey.ToString());

                    foreach (var ch in group.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase))
                    {
                        MenuHelper.AddChildChannelItem(subMenu, ch, channelClick);
                    }

                    if (subMenu.DropDownItems.Count > 0)
                    {
                        root.DropDownItems.Add(subMenu);
                    }
                }
            }
            else
            {
                foreach (var ch in Channels)
                {
                    MenuHelper.AddChildChannelItem(root, ch, channelClick);
                }
            }

            if (root.DropDownItems.Count > 0)
            {
                menu.Items.Add(root);
                _added.Add(root);
            }
        }
    }
}