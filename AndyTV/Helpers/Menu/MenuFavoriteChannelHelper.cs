using AndyTV.Data.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    private readonly List<ToolStripItem> _added = [];

    private static readonly HashSet<string> favoritesURLCache = new(
        StringComparer.OrdinalIgnoreCase
    );

    public static bool IsDuplicate(Channel channel, bool showMessageBox = true)
    {
        var key = channel.Url?.Trim() ?? "";
        bool isDuplicate = key.Length > 0 && favoritesURLCache.Contains(key);

        if (isDuplicate && showMessageBox)
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

    // just a one-liner entry point
    public void ShowFavorites(bool show)
    {
        RemoveAdded();

        if (!show)
            return;

        var favorites = ChannelDataService.LoadFavoriteChannels() ?? [];

        favoritesURLCache.Clear();
        foreach (var f in favorites)
        {
            var key = f.Url?.Trim();
            if (!string.IsNullOrEmpty(key))
                favoritesURLCache.Add(key);
        }

        if (favorites.Count == 0)
            return;

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

    private void RemoveAdded()
    {
        if (_added.Count == 0)
            return;

        for (int i = _added.Count - 1; i >= 0; i--)
        {
            var it = _added[i];
            if (menu.Items.Contains(it))
                menu.Items.Remove(it);
        }

        _added.Clear();
    }
}