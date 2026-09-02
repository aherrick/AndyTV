namespace AndyTV.vNext;

// Brief "now playing" toast in the player's bottom-right corner. AutoSize keeps it
// crisp across DPI; the caller shows one at a time.
static class Toast
{
    private static readonly Font Font = new("Segoe UI", 14f, FontStyle.Bold);

    public static Form Show(Form owner, string message, int durationMs = 3000)
    {
        var label = new Label
        {
            Text = message,
            AutoSize = true,
            ForeColor = Color.Red,
            Font = Font,
            Padding = new Padding(30, 15, 30, 15),
        };

        var toast = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            BackColor = Color.White,
            ShowInTaskbar = false,
            TopMost = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Opacity = 0.95,
        };
        toast.Controls.Add(label);
        toast.Paint += (_, e) =>
            ControlPaint.DrawBorder(
                e.Graphics,
                toast.ClientRectangle,
                Color.Silver,
                ButtonBorderStyle.Solid
            );

        // Place bottom-right before showing (size is known from the label) to avoid a flash.
        var size = label.PreferredSize;
        var origin = owner.PointToScreen(Point.Empty);
        const int margin = 40;
        toast.Location = new Point(
            origin.X + owner.ClientSize.Width - size.Width - margin,
            origin.Y + owner.ClientSize.Height - size.Height - margin
        );

        toast.Shown += async (_, _) =>
        {
            await Task.Delay(durationMs);
            toast.Close();
        };
        toast.Show(owner);
        return toast;
    }
}
