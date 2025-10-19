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
    private readonly List<ToolStripItem> _favoritesItems = [];

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

        // Always clear the favorites section first
        ClearFavoritesSection();

        if (show)
        {
            ShowFavoritesSection();
        }
    }

    private void ClearFavoritesSection()
    {
        // Remove all favorites items from menu
        foreach (var item in _favoritesItems)
        {
            if (_menu.Items.Contains(item))
            {
                _menu.Items.Remove(item);
            }
        }
        _favoritesItems.Clear();

        // Hide the header trio
        foreach (var item in _trio)
        {
            item.Visible = false;
        }
    }

    private void ShowFavoritesSection()
    {
        // Load favorites
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
            return; // Header trio already hidden by ClearFavoritesSection
        }

        // Show the header trio
        foreach (var item in _trio)
        {
            item.Visible = true;
        }

        // Get insertion position (after header trio)
        int headerIndex = _menu.Items.IndexOf(_header);
        int insertIndex = headerIndex + 2;

        // Add favorites content
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
                    var item = MenuHelper.AddChannelItemAt(_menu, insertIndex++, ch, _clickHandler);
                    _favoritesItems.Add(item);
                }
                continue;
            }

            var (header, itemsInserted) = MenuHelper.AddCategoryHeaderAt(
                _menu,
                insertIndex,
                catGroup.Key.ToUpperInvariant()
            );
            _favoritesItems.Add(header);
            insertIndex += itemsInserted;

            var withGroup = catGroup.Where(ch => !string.IsNullOrWhiteSpace(ch.Group));
            var noGroup = catGroup.Where(ch => string.IsNullOrWhiteSpace(ch.Group));

            foreach (
                var ch in noGroup.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            )
            {
                var item = MenuHelper.AddChannelItemAt(_menu, insertIndex++, ch, _clickHandler);
                _favoritesItems.Add(item);
            }

            var byGroup = withGroup
                .GroupBy(ch => ch.Group!.Trim())
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var grp in byGroup)
            {
                var groupNode = new ToolStripMenuItem(grp.Key);
                _favoritesItems.Add(groupNode);
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
}