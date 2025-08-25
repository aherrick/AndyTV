using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    private readonly ToolStripMenuItem _header = MenuHelper.AddHeader(menu, "FAVORITES");

    public void RebuildFavoritesMenu()
    {
        var favorites = ChannelDataService.LoadFavoriteChannels();

        _ui.Post(
            _ =>
            {
                int headerIndex = menu.Items.IndexOf(_header);
                int insertIndex = headerIndex + 2; // header + separator

                // Clear existing favorites until next separator
                while (
                    insertIndex < menu.Items.Count
                    && menu.Items[insertIndex] is not ToolStripSeparator
                )
                {
                    menu.Items.RemoveAt(insertIndex);
                }

                // Insert favorites
                foreach (var ch in favorites)
                {
                    var item = new ToolStripMenuItem(ch.Name) { Tag = ch.Url };
                    item.Click += clickHandler;
                    menu.Items.Insert(insertIndex++, item);
                }
            },
            null
        );
    }
}