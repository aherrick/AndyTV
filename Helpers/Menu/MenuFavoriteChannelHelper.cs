using System.Text.Json;
using AndyTV.Helpers.UI;
using AndyTV.Models;

namespace AndyTV.Helpers.Menu;

public class MenuFavoriteChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    private readonly ToolStripMenuItem _header = MenuHelper.AddHeader(menu, "FAVORITES");

    public void RebuildFavoritesMenu()
    {
        if (!File.Exists(FavoriteChannelForm.FileName))
        {
            return; // no favorites file, nothing to do
        }

        List<Channel> favorites = [];
        try
        {
            var json = File.ReadAllText(FavoriteChannelForm.FileName);
            favorites = JsonSerializer.Deserialize<List<Channel>>(json) ?? [];
        }
        catch { }

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