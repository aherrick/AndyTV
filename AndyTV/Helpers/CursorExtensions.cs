namespace AndyTV;

static class CursorExtensions
{
    // A blank 1x1 icon.
    private static readonly Cursor HiddenCursor = new(new Bitmap(1, 1).GetHicon());

    // Hidden over fullscreen video, default otherwise.
    public static Cursor IdleCursor(this Form form) => form.IsFullscreen() ? HiddenCursor : Cursors.Default;
}
