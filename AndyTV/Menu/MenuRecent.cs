using AndyTV.Data.Services;

namespace AndyTV.Menu;

public class MenuRecent
{
    private readonly ContextMenuStrip _menu;
    private readonly EventHandler _clickHandler;
    private readonly SynchronizationContext _ui;
    private readonly IRecentChannelService _recentChannelService;

    // keep only the right separator to anchor insert position
    private readonly ToolStripSeparator _rightSep;

    // track only the items we add after the header
    private readonly List<ToolStripItem> _added = [];

    public MenuRecent(ContextMenuStrip menu, EventHandler clickHandler, SynchronizationContext ui, IRecentChannelService recentChannelService)
    {
        _menu = menu;
        _clickHandler = clickHandler;
        _ui = ui;
        _recentChannelService = recentChannelService;

        var (_, all) = MenuHelper.AddHeader(_menu, "RECENT");
        _rightSep = (ToolStripSeparator)all[2]; // left = all[0], header = all[1], right = all[2]
    }

    public void Rebuild()
    {
        var recents = _recentChannelService.GetRecentChannels();

        // Marshal to UI thread only if we're not already on it
        if (!ReferenceEquals(SynchronizationContext.Current, _ui))
        {
            _ui.Post(_ => Rebuild(), null);
            return;
        }

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
    }
}