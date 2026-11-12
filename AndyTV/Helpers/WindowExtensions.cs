namespace AndyTV;

static class WindowExtensions
{
    public static bool IsFullscreen(this Form form) =>
        form.FormBorderStyle == FormBorderStyle.None;

    // Returns what ExitFullscreen should restore: null for maximized, otherwise the previous bounds.
    public static Rectangle? EnterFullscreen(this Form form)
    {
        Rectangle? restore = form.WindowState == FormWindowState.Maximized ? null : form.Bounds;
        form.SetBorderless(Screen.PrimaryScreen.Bounds);
        return restore;
    }

    public static void ExitFullscreen(this Form form, Rectangle? restore)
    {
        form.FormBorderStyle = FormBorderStyle.Sizable;
        form.WindowState = restore is null ? FormWindowState.Maximized : FormWindowState.Normal;
        if (restore is { } bounds)
        {
            form.Bounds = bounds;
        }
    }

    // Borderless half-screen, so two windows can sit side by side.
    public static void SnapToHalf(this Form form, bool left)
    {
        var screen = Screen.PrimaryScreen.Bounds;
        var x = left ? screen.X : screen.X + (screen.Width / 2);
        form.SetBorderless(new Rectangle(x, screen.Y, screen.Width / 2, screen.Height));
    }

    private static void SetBorderless(this Form form, Rectangle bounds)
    {
        form.FormBorderStyle = FormBorderStyle.None;
        form.WindowState = FormWindowState.Normal;
        form.Bounds = bounds;
    }
}
