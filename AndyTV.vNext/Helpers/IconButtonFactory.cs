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

    // Action buttons on the left, Save & Close on the right.
    public static Panel BottomBar(Control closeButton, params Control[] actions)
    {
        var left = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
        };
        left.Controls.AddRange(actions);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
        };
        right.Controls.Add(closeButton);

        var bar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(8),
        };
        bar.Controls.Add(left);
        bar.Controls.Add(right);
        return bar;
    }
}
