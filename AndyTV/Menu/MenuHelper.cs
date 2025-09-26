using AndyTV.Data.Models;

namespace AndyTV.Menu;

public static class MenuHelper
{
    public static (ToolStripMenuItem Header, ToolStripItem[] All) AddHeader(
        ContextMenuStrip menu,
        string text
    )
    {
        var sepBefore = new ToolStripSeparator();
        var header = new ToolStripMenuItem(text);
        var sepAfter = new ToolStripSeparator();

        menu.Items.Add(sepBefore);
        menu.Items.Add(header);
        menu.Items.Add(sepAfter);

        return (header, new ToolStripItem[] { sepBefore, header, sepAfter });
    }

    // NEW: Category/label header at index (bold/disabled by default).
    public static ToolStripMenuItem AddCategoryHeaderAt(
        ContextMenuStrip menu,
        int index,
        string text,
        bool bold = true,
        bool enabled = false
    )
    {
        var item = new ToolStripMenuItem
        {
            Text = text,
            Font = bold ? new Font(SystemFonts.MenuFont, FontStyle.Bold) : SystemFonts.MenuFont,
            Enabled = enabled,
        };
        menu.Items.Insert(index, item);
        return item;
    }

    public static ToolStripMenuItem AddChildChannelItem(
        ToolStripMenuItem parent,
        Channel ch,
        EventHandler clickHandler,
        string displayText = null
    )
    {
        var item = new ToolStripMenuItem(displayText ?? ch.DisplayName) { Tag = ch };
        item.Click += clickHandler;
        parent.DropDownItems.Add(item);
        return item;
    }

    public static ToolStripMenuItem AddChannelItemAt(
        ContextMenuStrip menu,
        int index,
        Channel ch,
        EventHandler clickHandler,
        string displayText = null
    )
    {
        var item = new ToolStripMenuItem(displayText ?? ch.DisplayName) { Tag = ch };
        item.Click += clickHandler;
        menu.Items.Insert(index, item);
        return item;
    }

    public static ToolStripMenuItem AddMenuItem(
        ToolStripMenuItem parent,
        string text,
        EventHandler clickHandler = null
    )
    {
        var item = new ToolStripMenuItem(text);
        if (clickHandler is not null)
        {
            item.Click += clickHandler;
        }
        parent.DropDownItems.Add(item);
        return item;
    }

    public static ToolStripSeparator AddSeparator(ToolStripMenuItem parent)
    {
        var sep = new ToolStripSeparator();
        parent.DropDownItems.Add(sep);
        return sep;
    }
}