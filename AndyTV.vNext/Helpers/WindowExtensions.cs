namespace AndyTV.vNext;

static class WindowExtensions
{
    public static bool IsFullscreen(this Form form) =>
        form.FormBorderStyle == FormBorderStyle.None;

    public static void EnterFullscreen(this Form form)
    {
        form.FormBorderStyle = FormBorderStyle.None;
        form.WindowState = FormWindowState.Normal;
        form.Bounds = Screen.PrimaryScreen.Bounds;
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

    public static void ExitFullscreen(this Form form, FormWindowState state, Rectangle bounds)
    {
        form.FormBorderStyle = FormBorderStyle.Sizable;
        form.WindowState = state;
        if (state == FormWindowState.Normal)
        {
            form.Bounds = bounds;
        }
    }
}
