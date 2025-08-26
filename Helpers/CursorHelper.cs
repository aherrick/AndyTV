namespace AndyTV.Helpers;

public static class CursorExtensions
{
    private static readonly Cursor HiddenCursor = CreateHiddenCursor();

    private static Cursor CreateHiddenCursor()
    {
        using var bmp = new Bitmap(1, 1);
        IntPtr ptr = bmp.GetHicon();
        return new Cursor(ptr);
    }

    public static void ShowDefault(this Control control)
    {
        control.Cursor = Cursors.Default;
    }

    public static void ShowWaiting(this Control control)
    {
        control.Cursor = Cursors.WaitCursor;
    }

    public static void HideCursor(this Control control)
    {
        control.Cursor = HiddenCursor;
    }
}