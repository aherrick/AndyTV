using AndyTV.Data.Models;
using AndyTV.Data.Services;
using AndyTV.Helpers;
using AndyTV.Services;

namespace AndyTV.Menu;

public partial class MenuTop(ContextMenuStrip menu)
{
    private readonly List<ToolStripItem> _added = [];

    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    // Wrapper for legacy callers; prefer awaiting RebuildAsync.
    public void Rebuild(EventHandler channelClick)
    {
        _ = RebuildAsync(channelClick);
    }

    private async Task RebuildAsync(EventHandler channelClick)
    {
        // capture inputs once (avoid touching shared lists on worker thread)
        List<Channel> channels = PlaylistChannelService.Channels?.ToList() ?? [];

        // 1) Build the data plan OFF the UI thread
        var plan = await Task.Run(() =>
            {
                // ----- TOP CHANNELS plan -----
                var topPlan =
                    new List<(string CatName, string ParentName, List<Channel> Matches)>();
                void BuildTopPlan(string rootTitle, Dictionary<string, List<ChannelTop>> categories)
                {
                    foreach (
                        var (catName, entries) in categories.OrderBy(
                            k => k.Key,
                            StringComparer.OrdinalIgnoreCase
                        )
                    )
                    {
                        foreach (
                            var entry in entries.OrderBy(
                                e => e.Name,
                                StringComparer.OrdinalIgnoreCase
                            )
                        )
                        {
                            // Pre-filter off-thread (most expensive part)
                            var matches = channels
                                .Where(ch =>
                                    entry.Terms.Any(term =>
                                        ch.DisplayName.Contains(
                                            term,
                                            StringComparison.OrdinalIgnoreCase
                                        )
                                    )
                                )
                                .OrderBy(ch => ch.DisplayName, StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            if (matches.Count > 0)
                            {
                                topPlan.Add((catName, entry.Name, matches));
                            }
                        }
                    }
                }

                BuildTopPlan("US", ChannelService.TopUs());
                BuildTopPlan("UK", ChannelService.TopUk());

                // ----- 24/7 plan -----
                // We materialize ChannelService.Get247Entries off-thread;
                // UI items will be created later on UI thread.
                var plan247 = ChannelService.Get247Entries("24/7", channels);

                return (topPlan, plan247);
            })
            .ConfigureAwait(false);

        // 2) Apply minimal UI changes ON the UI thread, batched
        void ApplyUI()
        {
            if (!ReferenceEquals(SynchronizationContext.Current, _ui))
            {
                _ui.Post(_ => ApplyUI(), null);
                return;
            }

            // Clear previous section we added
            foreach (var it in _added)
            {
                if (menu.Items.Contains(it))
                {
                    menu.Items.Remove(it);
                }
            }
            _added.Clear();

            menu.SuspendLayout();
            try
            {
                // ----- TOP CHANNELS -----
                var (topHeader, topHeaderAll) = MenuHelper.AddHeader(menu, "TOP CHANNELS");
                _added.AddRange(topHeaderAll);

                // Build "US" and "UK" roots and fill from precomputed plan
                var rootUS = new ToolStripMenuItem("US");
                var rootUK = new ToolStripMenuItem("UK");

                // helper to add a (category -> parent -> channels) tree
                static void AddTopNodes(
                    ToolStripMenuItem root,
                    IEnumerable<(string CatName, string ParentName, List<Channel> Matches)> entries,
                    EventHandler click
                )
                {
                    // cat -> parent -> channels
                    var byCat = entries
                        .GroupBy(e => e.CatName, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                    foreach (var cat in byCat)
                    {
                        var catItem = new ToolStripMenuItem(cat.Key);
                        foreach (
                            var (CatName, ParentName, Matches) in cat.OrderBy(
                                e => e.ParentName,
                                StringComparer.OrdinalIgnoreCase
                            )
                        )
                        {
                            var parent = new ToolStripMenuItem(ParentName);
                            foreach (var ch in Matches)
                            {
                                MenuHelper.AddChildChannelItem(parent, ch, click);
                            }
                            catItem.DropDownItems.Add(parent);
                        }

                        if (catItem.DropDownItems.Count > 0)
                        {
                            root.DropDownItems.Add(catItem);
                        }
                    }
                }

                // Split plan into US/UK by intersecting with your TopUs/TopUk keys
                var usCats = new HashSet<string>(
                    ChannelService.TopUs().Keys,
                    StringComparer.OrdinalIgnoreCase
                );
                var ukCats = new HashSet<string>(
                    ChannelService.TopUk().Keys,
                    StringComparer.OrdinalIgnoreCase
                );

                var usPlan = plan.topPlan.Where(p => usCats.Contains(p.CatName));
                var ukPlan = plan.topPlan.Where(p => ukCats.Contains(p.CatName));

                AddTopNodes(rootUS, usPlan, channelClick);
                AddTopNodes(rootUK, ukPlan, channelClick);

                if (rootUS.DropDownItems.Count > 0)
                {
                    menu.Items.Add(rootUS);
                    _added.Add(rootUS);
                }
                if (rootUK.DropDownItems.Count > 0)
                {
                    menu.Items.Add(rootUK);
                    _added.Add(rootUK);
                }

                // ----- 24/7 -----
                var root247 = new ToolStripMenuItem("24/7");
                string currentBucket = null;
                ToolStripMenuItem currentMenu = null;

                foreach (var entry in plan.plan247)
                {
                    if (!string.Equals(entry.Bucket, currentBucket, StringComparison.Ordinal))
                    {
                        if (currentMenu != null && currentMenu.DropDownItems.Count > 0)
                        {
                            root247.DropDownItems.Add(currentMenu);
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
                        var subMenu = currentMenu
                            .DropDownItems.OfType<ToolStripMenuItem>()
                            .FirstOrDefault(m => m.Text == entry.GroupBase);

                        if (subMenu == null)
                        {
                            subMenu = new ToolStripMenuItem(entry.GroupBase);
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
                    root247.DropDownItems.Add(currentMenu);
                }

                if (root247.DropDownItems.Count > 0)
                {
                    menu.Items.Add(root247);
                    _added.Add(root247);
                }

                Logger.Info("[CHANNELS] Menu rebuilt (Top + 24/7)");
            }
            finally
            {
                menu.ResumeLayout(performLayout: false); // avoid immediate remeasure thrash
            }
        }

        ApplyUI();
    }

    public static Channel ChannelByUrl(string url)
    {
        return PlaylistChannelService.Channels.FirstOrDefault(ch =>
            !string.IsNullOrWhiteSpace(ch.Url)
            && string.Equals(ch.Url.Trim(), url.Trim(), StringComparison.OrdinalIgnoreCase)
        );
    }
}