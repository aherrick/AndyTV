using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper
{
    private readonly ContextMenuStrip _menu;
    private readonly EventHandler _clickHandler;
    private readonly ToolStripMenuItem _header;

    // static URLs for fast dupe check across the whole app
    private static readonly HashSet<string> s_favoriteUrlsCache = new(
        StringComparer.OrdinalIgnoreCase
    );

    public MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
    {
        _menu = menu;
        _clickHandler = clickHandler;
        _header = MenuHelper.AddHeader(_menu, "FAVORITES");
    }

    public static bool IsDuplicateUrlAndNotify(Channel channel)
    {
        bool isDuplicate = s_favoriteUrlsCache.Contains(channel.Url.Trim());
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

    public void RebuildFavoritesMenu()
    {
        var favorites = ChannelDataService.LoadFavoriteChannels() ?? [];

        // rebuild static URL set
        s_favoriteUrlsCache.Clear();
        foreach (var f in favorites)
        {
            s_favoriteUrlsCache.Add(f.Url.Trim());
        }

        int headerIndex = _menu.Items.IndexOf(_header);
        int insertIndex = headerIndex + 2;

        var leftSep = (ToolStripSeparator)_menu.Items[headerIndex - 1];
        var rightSep = (ToolStripSeparator)_menu.Items[headerIndex + 1];

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

        while (
            insertIndex < _menu.Items.Count && _menu.Items[insertIndex] is not ToolStripSeparator
        )
        {
            _menu.Items.RemoveAt(insertIndex);
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
                    item.Click += _clickHandler;
                    _menu.Items.Insert(insertIndex++, item);
                }
                continue;
            }

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

            foreach (
                var ch in noGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            )
            {
                var item = new ToolStripMenuItem(ch.DisplayName) { Tag = ch };
                item.Click += _clickHandler;
                _menu.Items.Insert(insertIndex++, item);
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
                    child.Click += _clickHandler;
                    groupNode.DropDownItems.Add(child);
                }

                _menu.Items.Insert(insertIndex++, groupNode);
            }
        }
    }
}