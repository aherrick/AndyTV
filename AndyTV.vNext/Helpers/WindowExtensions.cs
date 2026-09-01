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
