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
        var usRegion = ChannelMatcher.BuildTopRegion("US", ChannelService.TopUs(), channels);
        var ukRegion = ChannelMatcher.BuildTopRegion("UK", ChannelService.TopUk(), channels);
        var usItem = Render(usRegion, channelClick);
        var ukItem = Render(ukRegion, channelClick);
        var usCount = CountLeaves(usRegion);
        var ukCount = CountLeaves(ukRegion);
        var item247 = Render(ChannelMatcher.Build247(channels), channelClick);

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

                if (item247.DropDownItems.Count > 0)
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

    private static ToolStripMenuItem Render(MenuNode node, EventHandler channelClick)
    {
        if (node.Channel is { } channel)
        {
            var leaf = new ToolStripMenuItem(node.Text) { Tag = channel };
            leaf.Click += channelClick;
            return leaf;
        }

        var item = new ToolStripMenuItem(node.Text);
        if (node.Children is not null)
        {
            foreach (var child in node.Children)
            {
                item.DropDownItems.Add(Render(child, channelClick));
            }
        }
        return item;
    }

    private static int CountLeaves(MenuNode node)
    {
        if (node.Channel is not null)
        {
            return 1;
        }

        var total = 0;
        if (node.Children is not null)
        {
            foreach (var child in node.Children)
            {
                total += CountLeaves(child);
            }
        }
        return total;
    }

    private List<ToolStripMenuItem> BuildPlaylistItems(EventHandler channelClick)
    {
        var result = new List<ToolStripMenuItem>();
        foreach (var (playlist, channels) in playlistService.PlaylistChannels)
        {
            if (!playlist.ShowInMenu)
            {
                continue;
            }

            var root = new ToolStripMenuItem(playlist.Name);
            foreach (var node in ChannelMatcher.BuildPlaylistNodes(playlist, channels))
            {
                root.DropDownItems.Add(Render(node, channelClick));
            }

            if (root.DropDownItems.Count > 0)
            {
                result.Add(root);
            }
        }

        return result;
    }
}