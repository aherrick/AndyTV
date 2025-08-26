using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuRecentChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
{
    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    private readonly ToolStripMenuItem _header = MenuHelper.AddHeader(menu, "RECENT");

    public void RebuildRecentMenu()
    {
        var recents = RecentChannelsService.GetRecentChannels();

        _ui.Post(
            _ =>
            {
                int headerIndex = menu.Items.IndexOf(_header);
                int insertIndex = headerIndex + 2; // header + separator

                // Clear existing recents until next separator
                while (
                    insertIndex < menu.Items.Count
                    && menu.Items[insertIndex] is not ToolStripSeparator
                )
                {
                    menu.Items.RemoveAt(insertIndex);
                }

                // Insert current recents
                foreach (var ch in recents)
                {
                    var item = new ToolStripMenuItem(ch.DisplayName) { Tag = ch };
                    item.Click += clickHandler;
                    menu.Items.Insert(insertIndex++, item);
                }
            },
            null
        );
    }
}