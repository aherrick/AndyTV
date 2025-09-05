using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    private const string RegionStartName = "__FAV_REGION_START__";
    private const string RegionEndName = "__FAV_REGION_END__";

    // Cached bold font to avoid recreating per category header
    private static readonly Font BoldMenuFont = new(SystemFonts.MenuFont, FontStyle.Bold);

    public void RebuildFavoritesMenu()
    {
        var favorites = ChannelDataService.LoadFavoriteChannels();
        var (startIdx, endIdx) = EnsureRegion(menu);

        // Clear everything between START and END
        for (int i = endIdx - 1; i > startIdx; i--)
        {
            menu.Items.RemoveAt(i);
        }

        int pos = startIdx + 1;

        var byCategory = favorites
            .GroupBy(ch => string.IsNullOrWhiteSpace(ch.Category) ? null : ch.Category.Trim())
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var catGroup in byCategory)
        {
            if (catGroup.Key is null)
            {
                // Top-level (no category header)
                foreach (
                    var ch in catGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                )
                {
                    menu.Items.Insert(pos++, CreateChannelItem(ch, clickHandler));
                }
            }
            else
            {
                // Category header block
                menu.Items.Insert(pos++, new ToolStripSeparator());
                menu.Items.Insert(
                    pos++,
                    new ToolStripMenuItem
                    {
                        Text = catGroup.Key.ToUpperInvariant(),
                        Font = BoldMenuFont, // <-- cached
                        Enabled = false,
                    }
                );
                menu.Items.Insert(pos++, new ToolStripSeparator());

                // Items without group
                foreach (
                    var ch in catGroup
                        .Where(c => string.IsNullOrWhiteSpace(c.Group))
                        .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                )
                {
                    menu.Items.Insert(pos++, CreateChannelItem(ch, clickHandler));
                }

                // Items grouped into submenus
                foreach (
                    var grp in catGroup
                        .Where(c => !string.IsNullOrWhiteSpace(c.Group))
                        .GroupBy(c => c.Group!.Trim())
                        .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                )
                {
                    var node = new ToolStripMenuItem(grp.Key);

                    foreach (
                        var ch in grp.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                    )
                    {
                        node.DropDownItems.Add(CreateChannelItem(ch, clickHandler));
                    }

                    menu.Items.Insert(pos++, node);
                }
            }
        }
    }

    private static ToolStripMenuItem CreateChannelItem(Channel ch, EventHandler clickHandler)
    {
        var item = new ToolStripMenuItem(ch.DisplayName) { Tag = ch };
        item.Click += clickHandler;
        return item;
    }

    private static (int startIdx, int endIdx) EnsureRegion(ContextMenuStrip menu)
    {
        // START sentinel
        ToolStripSeparator start = menu
            .Items.OfType<ToolStripSeparator>()
            .FirstOrDefault(s => s.Name == RegionStartName);

        if (start is null)
        {
            start = new ToolStripSeparator { Name = RegionStartName };
            menu.Items.Add(start);
        }

        // END sentinel (keep invisible so it never shows)
        int startIdx = menu.Items.IndexOf(start);
        for (int i = startIdx + 1; i < menu.Items.Count; i++)
        {
            if (menu.Items[i] is ToolStripSeparator e && e.Name == RegionEndName)
            {
                e.Visible = false; // force hidden
                return (startIdx, i);
            }
        }

        // Add invisible end sentinel
        var end = new ToolStripSeparator { Name = RegionEndName, Visible = false };
        menu.Items.Add(end);
        return (startIdx, menu.Items.Count - 1);
    }
}