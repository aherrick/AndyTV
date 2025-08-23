namespace AndyTV.Helpers;

public static class CursorHelper
{
    private static bool _cursorShown = true;

    public static void Hide()
    {
        ExecuteOnUIThread(() =>
        {
            Application.UseWaitCursor = false;
            CursorShown = false;
        });
    }

    public static void ShowDefault()
    {
        ExecuteOnUIThread(() =>
        {
            Application.UseWaitCursor = false;
            CursorShown = true;
        });
    }

    public static void ShowWaiting()
    {
        ExecuteOnUIThread(() =>
        {
            Application.UseWaitCursor = true;
            CursorShown = true;
        });
    }

    private static void ExecuteOnUIThread(Action action)
    {
        var form = Application.OpenForms.Cast<Form>().FirstOrDefault();

        if (form?.InvokeRequired == true)
        {
            form.Invoke(action);
        }
        else if (form != null)
        {
            action(); // We have a form and we're on UI thread
        }
        else
        {
            // No forms available - defer execution until UI is ready
            void idleHandler(object s, EventArgs e)
            {
                Application.Idle -= idleHandler; // Remove self
                action();
            }

            Application.Idle += idleHandler;
        }
    }

    private static bool CursorShown
    {
        get => _cursorShown;
        set
        {
            if (value == _cursorShown)
                return;

            if (value)
                Cursor.Show();
            else
                Cursor.Hide();

            _cursorShown = value;
        }
    }
}