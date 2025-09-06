using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    // MenuHelper.AddHeader adds: [sep][header][sep]
    private readonly ToolStripMenuItem _header = MenuHelper.AddHeader(menu, "FAVORITES");

    public void RebuildFavoritesMenu()
    {
        var favorites = ChannelDataService.LoadFavoriteChannels();

        int headerIndex = menu.Items.IndexOf(_header);
        int insertIndex = headerIndex + 2;

        var leftSep = (ToolStripSeparator)menu.Items[headerIndex - 1];
        var rightSep = (ToolStripSeparator)menu.Items[headerIndex + 1];

        if (favorites.Count == 0)
        {
            leftSep.Visible = false;
            _header.Visible = false;
            rightSep.Visible = false;
            return;
        }

        leftSep.Visible = true;
        _header.Visible = true;
        rightSep.Visible = true;

        while (insertIndex < menu.Items.Count && menu.Items[insertIndex] is not ToolStripSeparator)
        {
            menu.Items.RemoveAt(insertIndex);
        }

        var byCategory = favorites
            .GroupBy(ch => string.IsNullOrWhiteSpace(ch.Category) ? null : ch.Category.Trim())
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var catGroup in byCategory)
        {
            if (catGroup.Key is null)
            {
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

            menu.Items.Insert(
                insertIndex++,
                new ToolStripMenuItem
                {
                    Text = catGroup.Key.ToUpperInvariant(),
                    Font = new Font(SystemFonts.MenuFont, FontStyle.Bold),
                    Enabled = false,
                }
            );

            var withGroup = catGroup.Where(ch => !string.IsNullOrWhiteSpace(ch.Group));
            var noGroup = catGroup.Where(ch => string.IsNullOrWhiteSpace(ch.Group));

            foreach (
                var ch in noGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            )
            {
                var item = new ToolStripMenuItem(ch.DisplayName) { Tag = ch };
                item.Click += clickHandler;
                menu.Items.Insert(insertIndex++, item);
            }

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