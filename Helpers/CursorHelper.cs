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