using AndyTV.Data.Models;
using AndyTV.Data.Services;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public partial class MenuTVChannelHelper(ContextMenuStrip menu)
{
    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    public List<Channel> Channels { get; private set; } = [];

    // ---- helper to create/wire/add a channel item in one place ----
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

    public async Task LoadAndBuildMenu(EventHandler channelClick, string m3uURL)
    {
        MenuHelper.AddHeader(menu, "TOP CHANNELS");

        var parsed = await Task.Run(() => M3UService.ParseM3U(m3uURL));
        Channels = [.. parsed.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)];

        var usTask = Task.Run(() => BuildTopMenu("US", ChannelService.TopUs(), channelClick));
        var ukTask = Task.Run(() => BuildTopMenu("UK", ChannelService.TopUk(), channelClick));
        var twentyFourSevenTask = Task.Run(() => Build247("24/7", channelClick));

        // TODO: consider parallelizing these if they become slow / also movie/tv
        //var movieVodTask = Task.Run(() => BuildVOD("Movie VOD", channelClick));
        //var tvVodTask = Task.Run(() => BuildVOD("TV VOD", channelClick));

        var topItems = await Task.WhenAll(
            usTask,
            ukTask,
            twentyFourSevenTask
        //movieVodTask,
        //tvVodTask
        );

        _ui.Post(_ => menu.Items.AddRange([.. topItems.Where(item => item != null)]), null);

        Logger.Info("[CHANNELS] Loaded");
    }

    private ToolStripMenuItem BuildVOD(string groupTitle, EventHandler channelClick)
    {
        var root = new ToolStripMenuItem(groupTitle);

        var channels = Channels
            .Where(ch => string.Equals(ch.Group, groupTitle, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ch => ch.DisplayName, StringComparer.OrdinalIgnoreCase);

        foreach (var ch in channels)
        {
            AddChannelItem(root, ch, channelClick);
        }

        return root.DropDownItems.Count > 0 ? root : null;
    }

    public ToolStripMenuItem Build247(string rootTitle, EventHandler channelClick)
    {
        var root = new ToolStripMenuItem(rootTitle);
        var entries = ChannelService.Get247Entries(rootTitle, Channels);

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

            if (entry.GroupBase == null) // Single
            {
                AddChannelItem(currentMenu, entry.Channel, channelClick, entry.DisplayText);
            }
            else // Grouped
            {
                var subMenu = currentMenu
                    .DropDownItems.OfType<ToolStripMenuItem>()
                    .FirstOrDefault(m => m.Text == entry.GroupBase);

                if (subMenu == null)
                {
                    subMenu = new ToolStripMenuItem(entry.GroupBase);
                    currentMenu.DropDownItems.Add(subMenu);
                }

                AddChannelItem(subMenu, entry.Channel, channelClick, entry.DisplayText);
            }
        }

        if (currentMenu != null && currentMenu.DropDownItems.Count > 0)
        {
            root.DropDownItems.Add(currentMenu);
        }

        return root;
    }

    private ToolStripMenuItem BuildTopMenu(
        string rootTitle,
        Dictionary<string, List<ChannelTop>> categories,
        EventHandler channelClick
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
                // account for short names (<=2 chars) needing exact match
                var matches = Channels
                    .Where(ch =>
                    {
                        foreach (var term in entry.Terms)
                        {
                            var isExact = term.Length <= 2;
                            if (
                                isExact
                                    ? string.Equals(
                                        ch.DisplayName,
                                        term,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                    : ch.DisplayName.Contains(
                                        term,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                            )
                            {
                                return true;
                            }
                        }
                        return false;
                    })
                    .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (matches.Count == 0)
                {
                    continue;
                }

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

        return rootItem.DropDownItems.Count > 0 ? rootItem : null;
    }

    public Channel ChannelByUrl(string url)
    {
        return Channels.FirstOrDefault(ch =>
            string.Equals(ch.Url.Trim(), url.Trim(), StringComparison.OrdinalIgnoreCase)
        );
    }
}