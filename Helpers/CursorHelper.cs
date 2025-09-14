﻿namespace AndyTV.Helpers;

public static class CursorExtensions
{
    private static readonly Cursor HiddenCursor = CreateHiddenCursor();

    private static Cursor CreateHiddenCursor()
    {
        using var bmp = new Bitmap(1, 1);
        return new Cursor(bmp.GetHicon());
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
            control.HideCursor();
        else
            control.ShowDefault();
    }
}