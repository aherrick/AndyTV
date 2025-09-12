using System.Diagnostics;

namespace AndyTV.Helpers;

public static class KeyboardHelper
{
    private const int POLL_TRIES = 10;
    private const int POLL_DELAY_MS = 120;

    public static void ShowOnScreenKeyboard()
    {
        Logger.Info("OSK start");

        // local poll helper (used multiple times)
        static bool WaitForOskMainWindow(int tries, int delayMs)
        {
            for (int i = 0; i < tries; i++)
            {
                try
                {
                    Process[] procs = Process.GetProcessesByName("osk");
                    foreach (Process p in procs)
                    {
                        p.Refresh();
                        if (p.MainWindowHandle != IntPtr.Zero)
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // ignore and keep polling
                }

                Thread.Sleep(delayMs);
            }

            return false;
        }

        if (Process.GetProcessesByName("osk").Length > 0)
        {
            if (WaitForOskMainWindow(POLL_TRIES, POLL_DELAY_MS))
            {
                Logger.Info("OSK visible");
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
                TryStart(pathSysnative, "", true) && WaitForOskMainWindow(POLL_TRIES, POLL_DELAY_MS)
            )
            {
                Logger.Info("OSK visible");
                return;
            }
        }

        if (File.Exists(pathSystem32))
        {
            if (TryStart(pathSystem32, "", true) && WaitForOskMainWindow(POLL_TRIES, POLL_DELAY_MS))
            {
                Logger.Info("OSK visible");
                return;
            }
        }

        if (
            TryStart("cmd.exe", "/c start \"\" \"osk.exe\"", false)
            && WaitForOskMainWindow(POLL_TRIES, POLL_DELAY_MS)
        )
        {
            Logger.Info("OSK visible");
            return;
        }

        if (TryStart("osk.exe", "", true) && WaitForOskMainWindow(POLL_TRIES, POLL_DELAY_MS))
        {
            Logger.Info("OSK visible");
            return;
        }

        Logger.Error("OSK did not appear");
    }

    private static bool TryStart(string fileName, string arguments, bool useShell)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = useShell,
                CreateNoWindow = !useShell,
                WindowStyle = useShell ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
            };

            Process p = Process.Start(psi);
            string pid = p != null ? p.Id.ToString() : "unknown";
            Logger.Info("OSK launch ok: " + fileName + " pid=" + pid);
            return true;
        }
        catch
        {
            Logger.Warn("OSK launch failed: " + fileName);
            return false;
        }
    }
}