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
                var firstCharGroups = Channels
                    .GroupBy(ch => char.ToUpperInvariant(ch.DisplayName.FirstOrDefault()))
                    .OrderBy(g => g.Key);

                // Decide whether this playlist should have an extra "title" level.
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
                        // Simple two-level: letter -> raw entries
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
                menu.Items.Add(root);
                _added.Add(root);
            }
        }
    }
}