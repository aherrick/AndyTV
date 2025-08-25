using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuRecentChannelHelper(
    ContextMenuStrip menu,
    EventHandler clickHandler,
    RecentChannelsService recentChannelsService
)
{
    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    private readonly ToolStripMenuItem _header = MenuHelper.AddHeader(menu, "RECENT");

    public void AddOrPromote(Channel ch)
    {
        recentChannelsService.AddOrPromote(ch);
        RebuildRecentMenu();
    }

    public void RebuildRecentMenu()
    {
        var recents = recentChannelsService.GetRecentChannels();

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
                    var item = new ToolStripMenuItem(ch.Name) { Tag = ch.Url };
                    item.Click += clickHandler;
                    menu.Items.Insert(insertIndex++, item);
                }
            },
            null
        );
    }

    public Channel GetPrevious()
    {
        return recentChannelsService.GetPrevious();
    }
}