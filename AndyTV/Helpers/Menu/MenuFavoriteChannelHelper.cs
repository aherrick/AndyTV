using AndyTV.Data.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu
{
    public class MenuFavoriteChannelHelper
    {
        private readonly ContextMenuStrip _menu;
        private readonly EventHandler _clickHandler;

        private ToolStripSeparator _anchorStart;
        private ToolStripSeparator _anchorEnd;

        private static readonly HashSet<string> _favoritesUrlCache = new(
            StringComparer.OrdinalIgnoreCase
        );

        public MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
        {
            _menu = menu;
            _clickHandler = clickHandler;
        }

        // Call once from Form1 to create the invisible "slot".
        public void EnsureAnchors()
        {
            _anchorStart = new ToolStripSeparator { Tag = "FAV_ANCHOR_START", Visible = false };
            _anchorEnd = new ToolStripSeparator { Tag = "FAV_ANCHOR_END", Visible = false };

            _menu.Items.Add(_anchorStart);
            _menu.Items.Add(_anchorEnd);
        }

        // Assumes anchors exist (no guards by design).
        public void ShowFavorites(bool show)
        {
            int s = _menu.Items.IndexOf(_anchorStart);
            int e = _menu.Items.IndexOf(_anchorEnd);

            // Clear everything between anchors inline
            for (int i = e - 1; i > s; i--)
            {
                _menu.Items.RemoveAt(i);
            }

            if (!show)
                return;

            var favorites = ChannelDataService.LoadFavoriteChannels() ?? [];

            _favoritesUrlCache.Clear();
            foreach (var f in favorites)
            {
                var key = f.Url?.Trim();
                if (!string.IsNullOrEmpty(key))
                    _favoritesUrlCache.Add(key);
            }

            if (favorites.Count == 0)
                return;

            int at = s + 1;

            // FAVORITES header trio at index using helper
            var (Header, All) = MenuHelper.AddHeaderAt(_menu, at, "FAVORITES");
            at += All.Length; // sep + header + sep

            // Build by Category; null/empty category first
            var byCategory = favorites
                .GroupBy(ch => string.IsNullOrWhiteSpace(ch.Category) ? null : ch.Category.Trim())
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var catGroup in byCategory)
            {
                if (catGroup.Key is null)
                {
                    // Ungrouped category: just items
                    foreach (
                        var ch in catGroup.OrderBy(
                            c => c.DisplayName,
                            StringComparer.OrdinalIgnoreCase
                        )
                    )
                    {
                        MenuHelper.AddChannelItemAt(_menu, at++, ch, _clickHandler);
                    }
                }
                else
                {
                    // Category header (bold, disabled) at index using helper
                    MenuHelper.AddCategoryHeaderAt(_menu, at++, catGroup.Key!.ToUpperInvariant());

                    var withGroup = catGroup.Where(ch => !string.IsNullOrWhiteSpace(ch.Group));
                    var noGroup = catGroup.Where(ch => string.IsNullOrWhiteSpace(ch.Group));

                    // Ungrouped items first
                    foreach (
                        var ch in noGroup.OrderBy(
                            c => c.DisplayName,
                            StringComparer.OrdinalIgnoreCase
                        )
                    )
                    {
                        MenuHelper.AddChannelItemAt(_menu, at++, ch, _clickHandler);
                    }

                    // Grouped items by Group label
                    var byGroup = withGroup
                        .GroupBy(ch => ch.Group!.Trim())
                        .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                    foreach (var grp in byGroup)
                    {
                        var groupNode = new ToolStripMenuItem(grp.Key);
                        foreach (
                            var ch in grp.OrderBy(
                                c => c.DisplayName,
                                StringComparer.OrdinalIgnoreCase
                            )
                        )
                        {
                            MenuHelper.AddChildChannelItem(groupNode, ch, _clickHandler);
                        }
                        _menu.Items.Insert(at++, groupNode);
                    }
                }
            }
        }

        public static bool IsDuplicate(Channel channel, bool showMessageBox = true)
        {
            var key = channel.Url?.Trim() ?? string.Empty;
            bool isDuplicate = key.Length > 0 && _favoritesUrlCache.Contains(key);

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
    }
}