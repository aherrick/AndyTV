using System.Diagnostics;

namespace AndyTV.Helpers;

public static class UtilHelper
{
    // Generic launcher with concise logging
    public static bool TryStart(string fileName, string arguments, bool useShell, string label)
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
            Logger.Info("PROC ok: " + label + " file='" + fileName + "' pid=" + pid);
            return true;
        }
        catch
        {
            Logger.Warn("PROC fail: " + label + " file='" + fileName + "'");
            return false;
        }
    }

    public static bool WaitForMainWindow(string processName, int tries, int delayMs)
    {
        for (int i = 0; i < tries; i++)
        {
            try
            {
                Process[] procs = Process.GetProcessesByName(processName);
                foreach (Process p in procs)
                {
                    p.Refresh();
                    if (p.MainWindowHandle != nint.Zero)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // ignore and keep polling
            }

            try
            {
                Thread.Sleep(delayMs);
            }
            catch { }
        }

        return false;
    }
}