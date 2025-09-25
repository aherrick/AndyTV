namespace AndyTV.Helpers.Menu;

public static class MenuHelper
{
    public static ToolStripMenuItem AddHeader(ContextMenuStrip menu, string text)
    {
        menu.Items.Add(new ToolStripSeparator());

        var topHeader = new ToolStripMenuItem
        {
            Text = text,
            Font = new Font(SystemFonts.MenuFont, FontStyle.Bold),
        };
        menu.Items.Add(topHeader);

        menu.Items.Add(new ToolStripSeparator());

        return topHeader;
    }
}