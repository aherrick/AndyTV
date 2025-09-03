using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    private readonly ToolStripMenuItem _header = MenuHelper.AddHeader(menu, "FAVORITES");

    public void RebuildFavoritesMenu()
    {
        var favorites = ChannelDataService.LoadFavoriteChannels();

        int headerIndex = menu.Items.IndexOf(_header);
        int insertIndex = headerIndex + 2; // header + separator

        // Clear existing favorites until the next separator
        while (insertIndex < menu.Items.Count && menu.Items[insertIndex] is not ToolStripSeparator)
            menu.Items.RemoveAt(insertIndex);

        // Group everything by Category (null/empty treated as top-level)
        var byCategory = favorites
            .GroupBy(ch => string.IsNullOrWhiteSpace(ch.Category) ? null : ch.Category.Trim())
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase); // null (top-level) will sort first

        foreach (var catGroup in byCategory)
        {
            if (catGroup.Key is null)
            {
                // Top-level items (no category header)
                foreach (
                    var ch in catGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                )
                {
                    var item = new ToolStripMenuItem(ch.DisplayName) { Tag = ch };
                    item.Click += clickHandler;
                    menu.Items.Insert(insertIndex++, item);
                }
                continue;
            }

            // Category header
            menu.Items.Insert(
                insertIndex++,
                new ToolStripMenuItem
                {
                    Text = catGroup.Key.ToUpperInvariant(),
                    Font = new Font(SystemFonts.MenuFont, FontStyle.Bold),
                    Enabled = false,
                }
            );

            // Split into with/without Group inside this category
            var withGroup = catGroup.Where(ch => !string.IsNullOrWhiteSpace(ch.Group));
            var noGroup = catGroup.Where(ch => string.IsNullOrWhiteSpace(ch.Group));

            // Direct items (no group) under the category header
            foreach (
                var ch in noGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            )
            {
                var item = new ToolStripMenuItem(ch.DisplayName) { Tag = ch };
                item.Click += clickHandler;
                menu.Items.Insert(insertIndex++, item);
            }

            // Grouped submenus
            var byGroup = withGroup
                .GroupBy(ch => ch.Group!.Trim())
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var grp in byGroup)
            {
                var groupNode = new ToolStripMenuItem(grp.Key);
                foreach (
                    var ch in grp.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                )
                {
                    var child = new ToolStripMenuItem(ch.DisplayName) { Tag = ch };
                    child.Click += clickHandler;
                    groupNode.DropDownItems.Add(child);
                }
                menu.Items.Insert(insertIndex++, groupNode);
            }
        }
    }
}