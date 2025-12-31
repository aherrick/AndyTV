using System.Runtime.InteropServices;

namespace AndyTV.Helpers;

public static class CursorExtensions
{
#pragma warning disable SYSLIB1054 // Use LibraryImport - not worth enabling unsafe blocks for one call

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(nint hObject);

#pragma warning restore SYSLIB1054

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