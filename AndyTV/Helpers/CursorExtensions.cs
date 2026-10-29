using System.Runtime.InteropServices;

namespace AndyTV;

static class CursorExtensions
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

    // Hidden over fullscreen video, default otherwise.
    public static Cursor IdleCursor(this Form form) => form.IsFullscreen() ? HiddenCursor : Cursors.Default;
}
