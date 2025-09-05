using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    private const string RegionStartName = "__FAV_REGION_START__";
    private const string RegionEndName = "__FAV_REGION_END__";

    private readonly ToolStripMenuItem _header = MenuHelper.AddHeader(menu, "FAVORITES");

    public void RebuildFavoritesMenu()
    {
        var favorites = ChannelDataService.LoadFavoriteChannels();
        var (startIdx, endIdx) = EnsureRegion(menu, _header);

        // Clear everything between START and END (exclusive)
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
                foreach (
                    var ch in catGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                )
                {
                    menu.Items.Insert(pos++, CreateChannelItem(ch, clickHandler));
                }
            }
            else
            {
                // Category header block: sep + HEADER + sep
                menu.Items.Insert(pos++, new ToolStripSeparator());
                menu.Items.Insert(
                    pos++,
                    new ToolStripMenuItem
                    {
                        Text = catGroup.Key.ToUpperInvariant(),
                        Font = new Font(SystemFonts.MenuFont, FontStyle.Bold),
                        Enabled = false,
                    }
                );
                menu.Items.Insert(pos++, new ToolStripSeparator());

                foreach (
                    var ch in catGroup
                        .Where(c => string.IsNullOrWhiteSpace(c.Group))
                        .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                )
                {
                    menu.Items.Insert(pos++, CreateChannelItem(ch, clickHandler));
                }

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

    private static (int startIdx, int endIdx) EnsureRegion(
        ContextMenuStrip menu,
        ToolStripMenuItem header
    )
    {
        int headerIndex = menu.Items.IndexOf(header);
        int afterHeader = headerIndex + 2;

        // START sentinel
        ToolStripSeparator start;
        if (
            afterHeader < menu.Items.Count
            && menu.Items[afterHeader] is ToolStripSeparator s
            && s.Name == RegionStartName
        )
        {
            start = s;
        }
        else
        {
            start = new ToolStripSeparator { Name = RegionStartName };
            menu.Items.Insert(afterHeader, start);
        }

        // END sentinel (always invisible)
        int startIdx = menu.Items.IndexOf(start);
        for (int i = startIdx + 1; i < menu.Items.Count; i++)
        {
            if (menu.Items[i] is ToolStripSeparator e && e.Name == RegionEndName)
            {
                e.Visible = false;
                return (startIdx, i);
            }
        }

        var end = new ToolStripSeparator { Name = RegionEndName, Visible = false };
        menu.Items.Add(end);
        return (startIdx, menu.Items.Count - 1);
    }
}