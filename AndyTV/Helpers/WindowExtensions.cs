namespace AndyTV;

static class WindowExtensions
{
    public static bool IsFullscreen(this Form form) =>
        form.FormBorderStyle == FormBorderStyle.None;

    // Borderless + maximized covers the window's current screen, and Windows remembers the size to
    // restore. Going through Normal makes Windows recompute the maximized size for the new border.
    public static void ToggleFullscreen(this Form form)
    {
        var border = form.IsFullscreen() ? FormBorderStyle.Sizable : FormBorderStyle.None;
        form.WindowState = FormWindowState.Normal;
        form.FormBorderStyle = border;
        form.WindowState = FormWindowState.Maximized;
    }

    // Borderless half-screen, so two windows can sit side by side.
    public static void SnapToHalf(this Form form, bool left)
    {
        form.FormBorderStyle = FormBorderStyle.None;
        form.WindowState = FormWindowState.Normal;
        var screen = Screen.PrimaryScreen.Bounds;
        var x = left ? screen.X : screen.X + (screen.Width / 2);
        form.Bounds = new Rectangle(x, screen.Y, screen.Width / 2, screen.Height);
    }
}
