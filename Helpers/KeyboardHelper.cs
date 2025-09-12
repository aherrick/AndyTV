using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using AndyTV.Helpers;

namespace AndyTV.Helpers;

public static class KeyboardHelper
{
    private const int POLL_TRIES = 12;
    private const int POLL_DELAY_MS = 120;

    public static void ShowOnScreenKeyboard()
    {
        Logger.Info("OSK: start");

        if (EnsureOskVisible(POLL_TRIES, POLL_DELAY_MS))
        {
            Logger.Info("OSK: visible.");
            return;
        }

        Logger.Error("OSK: failed to appear after all attempts.");
    }

    private static bool EnsureOskVisible(int tries, int delayMs)
    {
        if (IsProcessRunning("osk"))
        {
            Logger.Info("OSK already running; waiting for main window.");
            return WaitForOskMainWindow(tries, delayMs);
        }

        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string pathSystem32 = Path.Combine(winDir, "System32", "osk.exe");
        string pathSysnative = Path.Combine(winDir, "Sysnative", "osk.exe");

        // Prefer 64-bit osk.exe when running as 32-bit process on 64-bit OS
        if (
            !Environment.Is64BitProcess
            && Environment.Is64BitOperatingSystem
            && File.Exists(pathSysnative)
        )
        {
            if (TryStart(pathSysnative, "", true, "Launch OSK (Sysnative)"))
            {
                if (WaitForOskMainWindow(tries, delayMs))
                {
                    return true;
                }
            }
        }

        if (File.Exists(pathSystem32))
        {
            if (TryStart(pathSystem32, "", true, "Launch OSK (System32)"))
            {
                if (WaitForOskMainWindow(tries, delayMs))
                {
                    return true;
                }
            }
        }

        // Shell and PATH fallbacks
        if (TryStart("cmd.exe", "/c start \"\" \"osk.exe\"", false, "Launch OSK via cmd"))
        {
            if (WaitForOskMainWindow(tries, delayMs))
            {
                return true;
            }
        }

        if (TryStart("osk.exe", "", true, "Launch OSK (PATH)"))
        {
            if (WaitForOskMainWindow(tries, delayMs))
            {
                return true;
            }
        }

        Logger.Warn("OSK did not start.");
        return false;
    }

    private static bool WaitForOskMainWindow(int tries, int delayMs)
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
                        Logger.Info("OSK main window detected (pid=" + p.Id.ToString() + ").");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "OSK window poll failed (iteration " + (i + 1).ToString() + ").");
            }

            Sleep(delayMs);
        }

        Logger.Warn("OSK main window not detected after polling.");
        return false;
    }

    private static bool TryStart(string fileName, string arguments, bool useShell, string label)
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
            Logger.Info(label + " ok (file='" + fileName + "', pid=" + pid + ").");
            return true;
        }
        catch (Exception ex)
        {
            string argsForLog = string.IsNullOrEmpty(arguments) ? "<none>" : arguments;
            Logger.Error(ex, label + " failed (file='" + fileName + "', args=" + argsForLog + ")");
            return false;
        }
    }

    private static bool IsProcessRunning(string name)
    {
        try
        {
            return Process.GetProcessesByName(name).Length > 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Process enumeration failed for '" + name + "'.");
            return false;
        }
    }

    private static void Sleep(int ms)
    {
        try
        {
            Thread.Sleep(ms);
        }
        catch { }
    }
}