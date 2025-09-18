using System.Diagnostics;

namespace AndyTV.Helpers;

public static class KeyboardHelper
{
    private const int POLL_TRIES = 10;
    private const int POLL_DELAY_MS = 120;

    public static void ShowOnScreenKeyboard()
    {
        Logger.Info("OSK start (x64)");

        // If already running and its main window is up, we're done.
        if (Process.GetProcessesByName("osk").Length > 0)
        {
            if (UtilHelper.WaitForMainWindow("osk", POLL_TRIES, POLL_DELAY_MS))
            {
                Logger.Info("OSK visible via existing process");
                return;
            }
        }

        // x64 path only
        string oskPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "System32",
            "osk.exe"
        );

        // 1) Try explicit System32 path
        if (UtilHelper.TryStart(oskPath, "", true, "OSK System32"))
        {
            if (UtilHelper.WaitForMainWindow("osk", POLL_TRIES, POLL_DELAY_MS))
            {
                Logger.Info("OSK visible via System32");
                return;
            }
        }

        // 2) Try just "osk.exe" on PATH
        if (UtilHelper.TryStart("osk.exe", "", true, "OSK PATH"))
        {
            if (UtilHelper.WaitForMainWindow("osk", POLL_TRIES, POLL_DELAY_MS))
            {
                Logger.Info("OSK visible via PATH");
                return;
            }
        }

        // 3) Last resort: shell out to cmd start
        if (UtilHelper.TryStart("cmd.exe", "/c start \"\" \"osk.exe\"", false, "OSK cmd"))
        {
            if (UtilHelper.WaitForMainWindow("osk", POLL_TRIES, POLL_DELAY_MS))
            {
                Logger.Info("OSK visible via cmd");
                return;
            }
        }

        Logger.Error("OSK did not appear");
    }
}