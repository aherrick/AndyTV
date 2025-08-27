using AndyTV.Services;
using static System.Net.Mime.MediaTypeNames;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    private readonly ToolStripMenuItem _header = MenuHelper.AddHeader(menu, "FAVORITES");

    public void RebuildFavoritesMenu()
    {
        var favorites = ChannelDataService.LoadFavoriteChannels();

        _ui.Post(
            _ =>
            {
                int headerIndex = menu.Items.IndexOf(_header);
                int insertIndex = headerIndex + 2; // header + separator

                // Clear existing favorites until next separator
                while (
                    insertIndex < menu.Items.Count
                    && menu.Items[insertIndex] is not ToolStripSeparator
                )
                {
                    menu.Items.RemoveAt(insertIndex);
                }

                // Separate channels with and without categories
                var channelsWithoutCategory = favorites.Where(ch =>
                    string.IsNullOrWhiteSpace(ch.Category)
                );
                var channelsWithCategory = favorites.Where(ch =>
                    !string.IsNullOrWhiteSpace(ch.Category)
                );

                // Add channels without category directly to top level
                foreach (
                    var ch in channelsWithoutCategory.OrderBy(
                        c => c.MappedName ?? c.Name,
                        StringComparer.OrdinalIgnoreCase
                    )
                )
                {
                    var item = new ToolStripMenuItem(ch.MappedName ?? ch.Name) { Tag = ch };
                    item.Click += clickHandler;
                    menu.Items.Insert(insertIndex++, item);
                }

                // Group by Category
                var byCategory = channelsWithCategory
                    .GroupBy(ch => ch.Category)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var catGroup in byCategory)
                {
                    menu.Items.Insert(
                        insertIndex++,
                        new ToolStripMenuItem
                        {
                            Text = catGroup.Key.ToUpperInvariant(),
                            Font = new System.Drawing.Font(SystemFonts.MenuFont, FontStyle.Bold),
                            Enabled = false,
                        }
                    );

                    // Separate channels with and without groups within this category
                    var channelsWithGroups = catGroup.Where(ch =>
                        !string.IsNullOrWhiteSpace(ch.Group)
                    );
                    var channelsWithoutGroups = catGroup.Where(ch =>
                        string.IsNullOrWhiteSpace(ch.Group)
                    );

                    // Add channels without groups directly under category header
                    foreach (
                        var ch in channelsWithoutGroups.OrderBy(
                            c => c.MappedName ?? c.Name,
                            StringComparer.OrdinalIgnoreCase
                        )
                    )
                    {
                        var item = new ToolStripMenuItem(ch.MappedName ?? ch.Name) { Tag = ch };
                        item.Click += clickHandler;
                        menu.Items.Insert(insertIndex++, item);
                    }

                    // Group channels with groups
                    var byGroup = channelsWithGroups
                        .GroupBy(ch => ch.Group)
                        .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                    foreach (var grp in byGroup)
                    {
                        var groupNode = new ToolStripMenuItem(grp.Key);

                        // Channels under group
                        foreach (
                            var ch in grp.OrderBy(
                                c => c.MappedName ?? c.Name,
                                StringComparer.OrdinalIgnoreCase
                            )
                        )
                        {
                            var item = new ToolStripMenuItem(ch.MappedName ?? ch.Name) { Tag = ch };
                            item.Click += clickHandler;
                            groupNode.DropDownItems.Add(item);
                        }

                        menu.Items.Insert(insertIndex++, groupNode);
                    }
                }
            },
            null
        );
    }
}