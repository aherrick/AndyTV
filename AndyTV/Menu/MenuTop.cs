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
        // Build all menu items off the UI thread — this is the expensive part.
        var channels = playlistService.Channels;
        var playlistItems = BuildPlaylistItems(channelClick);
        var (usItem, usCount) = BuildTopItems("US", ChannelService.TopUs(), channelClick, channels);
        var (ukItem, ukCount) = BuildTopItems("UK", ChannelService.TopUk(), channelClick, channels);
        var item247 = Build247Items("24/7", channelClick, channels);

        // Only the quick swap runs on the UI thread.
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

                var (_, topAll) = MenuHelper.AddHeader(menu, "CHANNELS");
                _added.AddRange(topAll);

                foreach (var pi in playlistItems)
                {
                    menu.Items.Add(pi);
                    _added.Add(pi);
                }

                if (usItem.DropDownItems.Count > 0)
                {
                    menu.Items.Add(usItem);
                    _added.Add(usItem);
                }

                if (ukItem.DropDownItems.Count > 0)
                {
                    menu.Items.Add(ukItem);
                    _added.Add(ukItem);
                }

                if (item247 is not null)
                {
                    menu.Items.Add(item247);
                    _added.Add(item247);
                }

                Logger.Info(
                    $"[CHANNELS] Menu rebuilt – {channels.Count} channels, US={usCount} UK={ukCount}"
                );
            },
            null
        );
    }

    private static ToolStripMenuItem Build247Items(string rootTitle, EventHandler channelClick, List<Channel> channels)
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

        return root.DropDownItems.Count > 0 ? root : null;
    }

    private static (ToolStripMenuItem Root, int TotalMatches) BuildTopItems(
        string rootTitle,
        Dictionary<string, List<ChannelTop>> categories,
        EventHandler channelClick,
        List<Channel> channels
    )
    {
        var rootItem = new ToolStripMenuItem(rootTitle);
        var totalMatches = 0;

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
                        ch.DisplayName != null
                        && entry.Terms.Any(term =>
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

                totalMatches += matches.Count;

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

        return (rootItem, totalMatches);
    }

    private List<ToolStripMenuItem> BuildPlaylistItems(EventHandler channelClick)
    {
        var result = new List<ToolStripMenuItem>();
        var playlistChannelsMenu = playlistService.PlaylistChannels.Where(x =>
            x.Playlist.ShowInMenu
        );

        foreach (var (Playlist, Channels) in playlistChannelsMenu)
        {
            var root = new ToolStripMenuItem(Playlist.Name);

            if (Playlist.GroupByFirstChar)
            {
                var firstCharGroups = Channels
                    .GroupBy(ch => char.ToUpperInvariant(ch.DisplayName.FirstOrDefault()))
                    .OrderBy(g => g.Key);

                // Decide whether this playlist should have an extra "title" level
                // (episodic grouping) based on NameFind/NameReplace being set.
                var hasNameTransform =
                    !string.IsNullOrWhiteSpace(Playlist.NameFind)
                    && Playlist.NameReplace is not null;

                foreach (var firstCharGroup in firstCharGroups)
                {
                    var groupKey = firstCharGroup.Key;
                    if (!char.IsLetterOrDigit(groupKey))
                        groupKey = '#';

                    var firstCharMenu = new ToolStripMenuItem(groupKey.ToString());

                    if (hasNameTransform)
                    {
                        // Three-level: letter -> base title (DisplayName, case-insensitive) -> entries
                        var titleGroups = firstCharGroup
                            .GroupBy(
                                c => c.DisplayName,
                                StringComparer.OrdinalIgnoreCase
                            )
                            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                        foreach (var titleGroup in titleGroups)
                        {
                            // If there is only one channel for this title, avoid creating
                            // a one-item show submenu; just add the entry directly under
                            // the letter group.
                            if (titleGroup.Count() == 1)
                            {
                                var single = titleGroup.First();
                                MenuHelper.AddChildChannelItem(firstCharMenu, single, channelClick, single.RawName);
                                continue;
                            }

                            var titleMenu = new ToolStripMenuItem(titleGroup.Key);

                            foreach (var ch in titleGroup.OrderBy(
                                         c => c.DisplayName,
                                         StringComparer.OrdinalIgnoreCase
                                     ))
                            {
                                // For episode entries, use the raw name so the full
                                // title (including episode code) is visible.
                                MenuHelper.AddChildChannelItem(
                                    titleMenu,
                                    ch,
                                    channelClick,
                                    ch.RawName
                                );
                            }

                            if (titleMenu.DropDownItems.Count > 0)
                            {
                                firstCharMenu.DropDownItems.Add(titleMenu);
                            }
                        }
                    }
                    else
                    {
                        // Simple two-level: letter -> entries (using current display name)
                        foreach (var ch in firstCharGroup.OrderBy(
                                     c => c.DisplayName,
                                     StringComparer.OrdinalIgnoreCase
                                 ))
                        {
                            MenuHelper.AddChildChannelItem(firstCharMenu, ch, channelClick);
                        }
                    }

                    if (firstCharMenu.DropDownItems.Count > 0)
                    {
                        root.DropDownItems.Add(firstCharMenu);
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
                result.Add(root);
            }
        }

        return result;
    }
}