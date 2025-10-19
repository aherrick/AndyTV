using AndyTV.Data.Models;
using AndyTV.Services;

namespace AndyTV.Menu;

public class MenuFavorite
{
    private readonly ContextMenuStrip _menu;
    private readonly EventHandler _clickHandler;
    private readonly SynchronizationContext _ui;

    private readonly ToolStripMenuItem _header;
    private readonly ToolStripItem[] _trio; // [leftSep, header, rightSep]

    private static readonly HashSet<string> favoritesURLCache = new(
        StringComparer.OrdinalIgnoreCase
    );

    public MenuFavorite(ContextMenuStrip menu, EventHandler clickHandler, SynchronizationContext ui)
    {
        _menu = menu;
        _clickHandler = clickHandler;
        _ui = ui;

        var (Header, All) = MenuHelper.AddHeader(_menu, "FAVORITES");
        _header = Header;
        _trio = All;
    }

    public static bool IsDuplicate(Channel channel)
    {
        var url = channel.Url?.Trim();
        bool isDuplicate = !string.IsNullOrWhiteSpace(url) && favoritesURLCache.Contains(url);
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

    public void Rebuild(bool show = true)
    {
        // Marshal to UI thread if we're not already on it
        if (!ReferenceEquals(SynchronizationContext.Current, _ui))
        {
            _ui.Post(_ => Rebuild(show), null);
            return;
        }

        int headerIndex = _menu.Items.IndexOf(_header);
        if (headerIndex < 0)
        {
            return;
        }

        // Insert position: right after the right separator in the header trio
        int insertIndex = headerIndex + 2;

        // Clear items between header trio and the next major section header
        while (insertIndex < _menu.Items.Count)
        {
            var item = _menu.Items[insertIndex];

            // Stop if we hit a major section header (all caps text, not part of favorites)
            if (item is ToolStripMenuItem menuItem &&
                string.Equals(menuItem.Text, menuItem.Text.ToUpperInvariant(), StringComparison.Ordinal) &&
                menuItem.Text.Length > 3 && // Avoid short headers
                !string.IsNullOrEmpty(menuItem.Text) &&
                insertIndex > headerIndex + 2) // Ensure it's after our header
            {
                break;
            }

            _menu.Items.RemoveAt(insertIndex);
        }

        if (!show)
        {
            HeaderVisible(false);
            return;
        }

        var favorites = ChannelDataService.LoadFavoriteChannels() ?? [];
        favoritesURLCache.Clear();
        foreach (var f in favorites)
        {
            var url = f.Url?.Trim();
            if (!string.IsNullOrWhiteSpace(url))
            {
                favoritesURLCache.Add(url);
            }
        }

        if (favorites.Count == 0)
        {
            HeaderVisible(false);
            return;
        }

        HeaderVisible(true);

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
                    MenuHelper.AddChannelItemAt(_menu, insertIndex++, ch, _clickHandler);
                }
                continue;
            }

            var (header, itemsInserted) = MenuHelper.AddCategoryHeaderAt(_menu, insertIndex, catGroup.Key.ToUpperInvariant());
            insertIndex += itemsInserted;

            var withGroup = catGroup.Where(ch => !string.IsNullOrWhiteSpace(ch.Group));
            var noGroup = catGroup.Where(ch => string.IsNullOrWhiteSpace(ch.Group));

            foreach (
                var ch in noGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            )
            {
                MenuHelper.AddChannelItemAt(_menu, insertIndex++, ch, _clickHandler);
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
                    MenuHelper.AddChildChannelItem(groupNode, ch, _clickHandler);
                }
                _menu.Items.Insert(insertIndex++, groupNode);
            }
        }
    }

    private void HeaderVisible(bool visible)
    {
        foreach (var it in _trio)
        {
            it.Visible = visible;
        }
    }
}