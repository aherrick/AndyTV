using AndyTV.Data.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper
{
    private readonly ContextMenuStrip _menu;
    private readonly EventHandler _clickHandler;
    private readonly ToolStripMenuItem _header;

    // static URLs for fast de-dupe check across the whole app
    private static readonly HashSet<string> favoritesURLCache = new(
        StringComparer.OrdinalIgnoreCase
    );

    public MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
    {
        _menu = menu;
        _clickHandler = clickHandler;
        _header = MenuHelper.AddHeader(_menu, "FAVORITES").Header;
    }

    public static bool IsDuplicate(Channel channel)
    {
        bool isDuplicate = favoritesURLCache.Contains(channel.Url.Trim());
        if (isDuplicate)
        {
            MessageBox.Show(
                $"\"{channel.DisplayName}\" is already in Favorites.",
                "Already Added",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        return isDuplicate;
    }

    public void RebuildFavoritesMenu(bool show = true)
    {
        int headerIndex = _menu.Items.IndexOf(_header);
        if (headerIndex < 0)
            return; // header not found, nothing to rebuild

        int insertIndex = headerIndex + 2; // position right after [header] and its right separator
        var leftSep = (ToolStripSeparator)_menu.Items[headerIndex - 1];
        var rightSep = (ToolStripSeparator)_menu.Items[headerIndex + 1];

        // --- Clear any existing favorites block ---
        // Start at the first item after the header/separator
        // Keep removing until we hit the next separator (the end of this section)
        while (
            insertIndex < _menu.Items.Count && _menu.Items[insertIndex] is not ToolStripSeparator
        )
        {
            _menu.Items.RemoveAt(insertIndex);
        }

        // If hiding, just collapse the header + separators and bail
        if (!show)
        {
            leftSep.Visible = false;
            _header.Visible = false;
            rightSep.Visible = false;
            return;
        }

        // Load persisted favorites
        var favorites = ChannelDataService.LoadFavoriteChannels() ?? [];

        // rebuild static URL set for quick duplicate checks
        favoritesURLCache.Clear();
        foreach (var f in favorites)
            favoritesURLCache.Add(f.Url.Trim());

        // No favorites saved → hide the whole header section
        if (favorites.Count == 0)
        {
            leftSep.Visible = false;
            _header.Visible = false;
            rightSep.Visible = false;
            return;
        }

        // Favorites exist → make the header and separators visible again
        leftSep.Visible = true;
        _header.Visible = true;
        rightSep.Visible = true;

        // --- Rebuild visible favorites items ---
        var byCategory = favorites
            .GroupBy(ch => string.IsNullOrWhiteSpace(ch.Category) ? null : ch.Category.Trim())
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var catGroup in byCategory)
        {
            if (catGroup.Key is null)
            {
                // Favorites without a category → flat list
                foreach (
                    var ch in catGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                )
                {
                    var item = new ToolStripMenuItem(ch.DisplayName) { Tag = ch };
                    item.Click += _clickHandler;
                    _menu.Items.Insert(insertIndex++, item);
                }
                continue;
            }

            // Category header (bold, disabled)
            _menu.Items.Insert(
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

            // Favorites with no subgroup
            foreach (
                var ch in noGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            )
            {
                var item = new ToolStripMenuItem(ch.DisplayName) { Tag = ch };
                item.Click += _clickHandler;
                _menu.Items.Insert(insertIndex++, item);
            }

            // Favorites grouped under a "Group" label
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
                    child.Click += _clickHandler;
                    groupNode.DropDownItems.Add(child);
                }

                _menu.Items.Insert(insertIndex++, groupNode);
            }
        }
    }
}