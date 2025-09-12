using System.Diagnostics;

namespace AndyTV.Helpers;

public static class KeyboardHelper
{
    private const int POLL_TRIES = 10;
    private const int POLL_DELAY_MS = 120;

    public static void ShowOnScreenKeyboard()
    {
        Logger.Info("OSK start");

        // Already running?
        if (Process.GetProcessesByName("osk").Length > 0)
        {
            if (UtilHelper.WaitForMainWindow("osk", POLL_TRIES, POLL_DELAY_MS))
            {
                Logger.Info("OSK visible via existing process");
                return;
            }
        }

        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string pathSystem32 = Path.Combine(winDir, "System32", "osk.exe");
        string pathSysnative = Path.Combine(winDir, "Sysnative", "osk.exe");

        // Prefer 64-bit OSK when running as 32-bit process on 64-bit OS
        if (
            !Environment.Is64BitProcess
            && Environment.Is64BitOperatingSystem
            && File.Exists(pathSysnative)
        )
        {
            if (
                UtilHelper.TryStart(pathSysnative, "", true, "OSK Sysnative")
                && UtilHelper.WaitForMainWindow("osk", POLL_TRIES, POLL_DELAY_MS)
            )
            {
                Logger.Info("OSK visible via Sysnative");
                return;
            }
        }

        if (File.Exists(pathSystem32))
        {
            if (
                UtilHelper.TryStart(pathSystem32, "", true, "OSK System32")
                && UtilHelper.WaitForMainWindow("osk", POLL_TRIES, POLL_DELAY_MS)
            )
            {
                Logger.Info("OSK visible via System32");
                return;
            }
        }

        if (
            UtilHelper.TryStart("cmd.exe", "/c start \"\" \"osk.exe\"", false, "OSK cmd")
            && UtilHelper.WaitForMainWindow("osk", POLL_TRIES, POLL_DELAY_MS)
        )
        {
            Logger.Info("OSK visible via cmd");
            return;
        }

        if (
            UtilHelper.TryStart("osk.exe", "", true, "OSK PATH")
            && UtilHelper.WaitForMainWindow("osk", POLL_TRIES, POLL_DELAY_MS)
        )
        {
            Logger.Info("OSK visible via PATH");
            return;
        }

        Logger.Error("OSK did not appear");
    }
}