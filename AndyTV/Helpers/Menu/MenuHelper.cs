namespace AndyTV.Helpers.Menu;

public static class MenuHelper
{
    // Return all created items so callers can track & remove them later.
    public static ToolStripItem[] AddHeader(ContextMenuStrip menu, string text)
    {
        var before = new ToolStripSeparator();

        var header = new ToolStripMenuItem
        {
            Text = text,
            Font = new Font(SystemFonts.MenuFont, FontStyle.Bold),
        };

        var after = new ToolStripSeparator();

        menu.Items.Add(before);
        menu.Items.Add(header);
        menu.Items.Add(after);

        return [before, header, after];
    }
}