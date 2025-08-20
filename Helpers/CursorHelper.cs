namespace AndyTV.Helpers;

public class CursorHelper(Form form)
{
    public void Hide()
    {
        form.Invoke(
            new Action(() =>
            {
                form.Cursor = Cursors.Default;

                if (form.WindowState == FormWindowState.Maximized)
                {
                    CursorShown = false;
                }
            })
        );
    }

    public void ShowDefault()
    {
        ShowCursor(Cursors.Default);
    }

    public void ShowWaiting()
    {
        var center = new Point(form.ClientSize.Width / 2, form.ClientSize.Height / 2);

        Cursor.Position = form.PointToScreen(center);

        ShowCursor(Cursors.WaitCursor);
    }

    private void ShowCursor(Cursor cursorType)
    {
        form.Invoke(
            new Action(() =>
            {
                form.Cursor = cursorType;

                CursorShown = true;
            })
        );
    }

    private static bool _cursorShown = true;

    private static bool CursorShown
    {
        get { return _cursorShown; }
        set
        {
            if (value == _cursorShown)
            {
                return;
            }

            if (value)
            {
                Cursor.Show();
            }
            else
            {
                Cursor.Hide();
            }

            _cursorShown = value;
        }
    }
}