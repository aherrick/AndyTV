using FontAwesome.Sharp;

namespace AndyTV.vNext;

// Shared builders so both manager forms get the same icon buttons and bottom-bar layout.
static class IconButtonFactory
{
    private static readonly ToolTip Tips = new();

    public static IconButton Make(IconChar icon, Color color, string tooltip, EventHandler onClick)
    {
        var button = new IconButton
        {
            IconChar = icon,
            IconColor = color,
            IconFont = IconFont.Auto,
            IconSize = 20,
            Size = new Size(40, 32),
            Margin = new Padding(2, 0, 2, 0),
        };
        button.Click += onClick;
        Tips.SetToolTip(button, tooltip);
        return button;
    }

    // Action buttons on the left, Save & Close on the right; auto-height so nothing clips.
    public static Panel BottomBar(Control closeButton, params Control[] actions)
    {
        var left = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Left,
        };
        left.Controls.AddRange(actions);

        closeButton.Anchor = AnchorStyles.Right;

        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.Controls.Add(left, 0, 0);
        bar.Controls.Add(closeButton, 1, 0);
        return bar;
    }
}
