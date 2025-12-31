using System.Runtime.InteropServices;

namespace AndyTV.Helpers;

public static partial class CursorExtensions
{
    [LibraryImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(nint hObject);

    private static readonly Cursor HiddenCursor = CreateHiddenCursor();

    private static Cursor CreateHiddenCursor()
    {
        using var bmp = new Bitmap(1, 1);
        var handle = bmp.GetHicon();
        var cursor = new Cursor(handle);
        DeleteObject(handle);
        return cursor;
    }

    public static void ShowDefault(this Control control) => SetCursor(control, Cursors.Default);

    public static void ShowWaiting(this Control control) => SetCursor(control, Cursors.WaitCursor);

    public static void HideCursor(this Control control) => SetCursor(control, HiddenCursor);

    private static void SetCursor(Control control, Cursor cursor)
    {
        if (control.InvokeRequired)
            control.BeginInvoke(() => control.Cursor = cursor);
        else
            control.Cursor = cursor;
    }

    public static void SetCursorForCurrentView(this Control control)
    {
        var form = control.FindForm();
        if (form != null && form.FormBorderStyle == FormBorderStyle.None)
        {
            control.HideCursor();
        }
        else
        {
            control.ShowDefault();
        }
    }
}