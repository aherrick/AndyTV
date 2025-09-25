using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuRecentChannelHelper
{
    private readonly ContextMenuStrip _menu;
    private readonly EventHandler _clickHandler;

    // keep only the right separator to anchor insert position
    private readonly ToolStripSeparator _rightSep;

    // track only the items we add after the header
    private readonly List<ToolStripItem> _added = [];

    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    public MenuRecentChannelHelper(ContextMenuStrip menu, EventHandler clickHandler)
    {
        _menu = menu;
        _clickHandler = clickHandler;

        var (_, all) = MenuHelper.AddHeader(_menu, "RECENT");
        _rightSep = (ToolStripSeparator)all[2]; // left = all[0], header = all[1], right = all[2]
    }

    public void RebuildRecentMenu()
    {
        var recents = RecentChannelService.GetRecentChannels();

        _ui.Post(
            _ =>
            {
                // remove previously added items (header stays fixed)
                foreach (var it in _added)
                {
                    if (_menu.Items.Contains(it))
                    {
                        _menu.Items.Remove(it);
                    }
                }
                _added.Clear();

                // insert immediately after the right separator of the header trio
                int insertIndex = _menu.Items.IndexOf(_rightSep) + 1;

                foreach (var ch in recents)
                {
                    var item = MenuHelper.AddChannelItemAt(_menu, insertIndex++, ch, _clickHandler);
                    _added.Add(item);
                }
            },
            null
        );
    }
}