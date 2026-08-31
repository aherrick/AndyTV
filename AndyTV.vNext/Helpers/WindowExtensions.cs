namespace AndyTV.vNext;

static class WindowExtensions
{
    public static void EnterFullscreen(this Form form)
    {
        form.FormBorderStyle = FormBorderStyle.None;
        form.WindowState = FormWindowState.Normal;
        form.Bounds = Screen.PrimaryScreen.Bounds;
    }

    public static void ToggleFullscreen(this Form form, Rectangle userBounds)
    {
        if (form.IsFullscreen())
        {
            form.ExitFullscreen(userBounds);
        }
        else
        {
            form.EnterFullscreen();
        }
    }

    private static bool IsFullscreen(this Form form) =>
        form.FormBorderStyle == FormBorderStyle.None;

    private static void ExitFullscreen(this Form form, Rectangle userBounds)
    {
        form.FormBorderStyle = FormBorderStyle.Sizable;
        if (userBounds == Rectangle.Empty)
        {
            form.WindowState = FormWindowState.Maximized;
        }
        else
        {
            form.WindowState = FormWindowState.Normal;
            form.Bounds = userBounds;
        }
    }
}
