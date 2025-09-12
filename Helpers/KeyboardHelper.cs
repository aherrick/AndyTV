using System.Diagnostics;

namespace AndyTV.Helpers
{
    public static class KeyboardHelper
    {
        public static void ShowOnScreenKeyboard()
        {
            Logger.Info("Keyboard: start (OSK-first, no P/Invoke)");

            EnsureTextServices();

            if (EnsureOskVisible(tries: 12, delayMs: 120))
            {
                Logger.Info("Keyboard: OSK visible.");
                return;
            }

            Logger.Warn("Keyboard: OSK did not appear; trying TabTip fallback.");
            if (EnsureTabTipVisible(tries: 12, delayMs: 120))
            {
                Logger.Info("Keyboard: TabTip visible.");
                return;
            }

            Logger.Error("Keyboard: neither OSK nor TabTip became visible.");
        }

        // ---- OSK path (no P/Invoke) ----
        private static bool EnsureOskVisible(int tries, int delayMs)
        {
            if (IsProcessRunning("osk"))
            {
                Logger.Info("OSK already running.");
                return WaitForMainWindow("osk", tries, delayMs);
            }

            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string sys32 = Path.Combine(winDir, "System32", "osk.exe");
            string sysnat = Path.Combine(winDir, "Sysnative", "osk.exe");

            // 32-bit process on 64-bit OS -> prefer Sysnative to get real 64-bit osk.exe
            if (
                !Environment.Is64BitProcess
                && Environment.Is64BitOperatingSystem
                && File.Exists(sysnat)
            )
            {
                if (TryStart(sysnat, "", true, "Launch OSK (Sysnative)"))
                {
                    return WaitForMainWindow("osk", tries, delayMs);
                }
            }

            if (File.Exists(sys32))
            {
                if (TryStart(sys32, "", true, "Launch OSK (System32)"))
                {
                    return WaitForMainWindow("osk", tries, delayMs);
                }
            }

            // Shell + PATH fallbacks
            if (TryStart("cmd.exe", "/c start \"\" \"osk.exe\"", false, "Launch OSK via cmd"))
            {
                return WaitForMainWindow("osk", tries, delayMs);
            }

            if (TryStart("osk.exe", "", true, "Launch OSK (PATH)"))
            {
                return WaitForMainWindow("osk", tries, delayMs);
            }

            Logger.Warn("OSK did not start.");
            return false;
        }

        // ---- TabTip fallback (no P/Invoke/COM) ----
        private static bool EnsureTabTipVisible(int tries, int delayMs)
        {
            if (IsProcessRunning("TabTip"))
            {
                Logger.Info("TabTip already running.");
                return WaitForMainWindow("TabTip", tries, delayMs);
            }

            if (TryStart("tabtip.exe", "", true, "Start TabTip (PATH)"))
            {
                return WaitForMainWindow("TabTip", tries, delayMs);
            }

            string path1 = @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe";
            string path2 = @"C:\Program Files (x86)\Common Files\microsoft shared\ink\TabTip.exe";

            if (File.Exists(path1) && TryStart(path1, "", true, "Start TabTip (x64)"))
            {
                return WaitForMainWindow("TabTip", tries, delayMs);
            }

            if (File.Exists(path2) && TryStart(path2, "", true, "Start TabTip (x86)"))
            {
                return WaitForMainWindow("TabTip", tries, delayMs);
            }

            Logger.Warn("TabTip did not start.");
            return false;
        }

        // ---- Generic helpers (no native calls) ----
        private static bool WaitForMainWindow(string processName, int tries, int delayMs)
        {
            for (int i = 0; i < tries; i++)
            {
                Process[] procs = Process.GetProcessesByName(processName);
                foreach (Process p in procs)
                {
                    try
                    {
                        p.Refresh();
                        if (p.MainWindowHandle != IntPtr.Zero)
                        {
                            Logger.Info(
                                processName + " main window detected (pid=" + p.Id.ToString() + ")."
                            );
                            return true;
                        }
                    }
                    catch
                    {
                        // Ignore transient access issues
                    }
                }
                Sleep(delayMs);
            }

            Logger.Warn(processName + " main window not detected after polling.");
            return false;
        }

        private static void EnsureTextServices()
        {
            try
            {
                if (!IsProcessRunning("ctfmon"))
                {
                    string ctf = Path.Combine(Environment.SystemDirectory, "ctfmon.exe");
                    TryStart(ctf, "", true, "Start ctfmon.exe");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "EnsureTextServices failed.");
            }
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
                Logger.Error(
                    ex,
                    label + " failed (file='" + fileName + "', args=" + argsForLog + ")"
                );
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
}