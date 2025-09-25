using AndyTV.Data.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    // track everything we add so we can nuke it cleanly
    private readonly List<ToolStripItem> _added = [];

    // static URLs for fast dupe check across the whole app
    private static readonly HashSet<string> favoritesURLCache = new(
        StringComparer.OrdinalIgnoreCase
    );

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

    public void RebuildFavoritesMenu()
    {
        // Clear out anything we previously added
        foreach (var it in _added)
        {
            if (menu.Items.Contains(it))
            {
                menu.Items.Remove(it);
            }
        }
        _added.Clear();

        var favorites = ChannelDataService.LoadFavoriteChannels() ?? [];

        // rebuild static URL set
        favoritesURLCache.Clear();
        foreach (var f in favorites)
        {
            favoritesURLCache.Add(f.Url.Trim());
        }

        if (favorites.Count == 0)
        {
            return; // nothing to add
        }

        // ----- FAVORITES HEADER -----
        var (_, allHeaderItems) = MenuHelper.AddHeader(menu, "FAVORITES");
        _added.AddRange(allHeaderItems);

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
                    var item = MenuHelper.AddChannelItem(menu, ch, clickHandler);
                    _added.Add(item);
                }
            }
            else
            {
                var catHeader = new ToolStripMenuItem
                {
                    Text = catGroup.Key.ToUpperInvariant(),
                    Font = new Font(SystemFonts.MenuFont, FontStyle.Bold),
                    Enabled = false,
                };
                menu.Items.Add(catHeader);
                _added.Add(catHeader);

                var withGroup = catGroup.Where(ch => !string.IsNullOrWhiteSpace(ch.Group));
                var noGroup = catGroup.Where(ch => string.IsNullOrWhiteSpace(ch.Group));

                foreach (
                    var ch in noGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                )
                {
                    var item = MenuHelper.AddChannelItem(menu, ch, clickHandler);
                    _added.Add(item);
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

                    menu.Items.Add(groupNode);
                    _added.Add(groupNode);
                }
            }
        }
    }
}