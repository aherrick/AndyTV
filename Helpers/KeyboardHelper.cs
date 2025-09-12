using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32;
using AndyTV.Helpers;

namespace AndyTV.Helpers;

public static class KeyboardHelper
{
    /// <summary>
    /// Attempts to show the Windows touch keyboard (TabTip). Falls back to OSK if needed.
    /// Logs each concrete action and failure reason.
    /// </summary>
    public static void ShowOnScreenKeyboard(IntPtr? parentHwnd = null, int visibleWaitMs = 1500)
    {
        Logger.Info("ShowOnScreenKeyboard: invoked");

        // 0) Best effort: ensure service is running (won't throw if not allowed)
        EnsureTabletInputServiceRunning();

        // 1) Try TabTip via PATH
        try
        {
            Logger.Info("Attempt: launch 'tabtip.exe' via PATH");
            Process.Start("tabtip.exe");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Start failed: 'tabtip.exe' via PATH (not in PATH or access denied)");
        }

        // 2) Try TabTip via full path(s) if not active yet
        if (!WaitForKeyboardToAppear(visibleWaitMs))
        {
            try
            {
                var path64 = @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe";
                var path32 = @"C:\Program Files (x86)\Common Files\microsoft shared\ink\TabTip.exe";
                Logger.Info($"Check: exists? '{path64}' => {File.Exists(path64)}; '{path32}' => {File.Exists(path32)}");

                var launchPath = File.Exists(path64) ? path64 : (File.Exists(path32) ? path32 : null);
                if (launchPath is null)
                {
                    Logger.Warn("Skip: TabTip.exe not found in expected locations; cannot launch by full path");
                }
                else
                {
                    Logger.Info($"Attempt: launch TabTip from full path '{launchPath}'");
                    var p = Process.Start(launchPath);
                    Logger.Info($"Result: TabTip started from full path (pid={p?.Id.ToString() ?? "unknown"})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Start failed: TabTip from full path");
            }
        }

        // 3) Wait briefly for TabTip/TextInputHost to actually surface
        if (WaitForKeyboardToAppear(visibleWaitMs))
        {
            Logger.Info("Success: touch keyboard process active after TabTip launch.");
            return;
        }

        // 4) As a nudge: enable desktop auto-invoke (per-user) if off (best-effort, non-fatal)
        TryEnableDesktopModeAutoInvoke();

        // 5) Try OSK via cmd (to avoid elevation prompts)
        try
        {
            Logger.Info("Attempt: launch OSK via cmd.exe (`/c start \"\" \"osk.exe\"`)");
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c start \"\" \"osk.exe\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            var p = Process.Start(psi);
            Logger.Info($"Result: cmd.exe invoked to start OSK (cmd pid={p?.Id.ToString() ?? "unknown"})");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Start failed: OSK via cmd.exe");
        }

        if (WaitForKeyboardToAppear(visibleWaitMs, includeOsk: true))
        {
            Logger.Info("Success: OSK active.");
            return;
        }

        // 6) Final fallback: direct OSK
        try
        {
            Logger.Info("Attempt: launch 'osk.exe' directly");
            var p = Process.Start("osk.exe");
            Logger.Info($"Result: started 'osk.exe' directly (pid={p?.Id.ToString() ?? "unknown"})");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Start failed: 'osk.exe' directly");
        }

        if (!WaitForKeyboardToAppear(visibleWaitMs, includeOsk: true))
        {
            Logger.Warn("Exhausted attempts: keyboard still not visible.");
        }
    }

    /// <summary>
    /// Polls briefly to see if the keyboard is likely "active".
    /// For TabTip on Win11, TextInputHost.exe is typically spawned alongside/after TabTip.
    /// </summary>
    private static bool WaitForKeyboardToAppear(int timeoutMs, bool includeOsk = false)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            // TabTip process may exist without visual UI; TextInputHost tends to indicate the modern UI pipeline.
            var tabTip = Process.GetProcessesByName("TabTip").Length;
            var tih = Process.GetProcessesByName("TextInputHost").Length;
            var osk = includeOsk ? Process.GetProcessesByName("osk").Length : 0;

            Logger.Info($"Probe: TabTip={tabTip}, TextInputHost={tih}" + (includeOsk ? $", OSK={osk}" : ""));

            // Heuristic: consider it "appeared" if TextInputHost is present or OSK is present (when requested)
            if (tih > 0 || (includeOsk && osk > 0))
            {
                return true;
            }

            Thread.Sleep(150);
        }
        return false;
    }

    /// <summary>
    /// Starts the "Touch Keyboard and Handwriting Panel Service" if it's stopped.
    /// No-ops on exceptions or access issues; logs outcome.
    /// </summary>
    private static void EnsureTabletInputServiceRunning()
    {
        try
        {
            using var sc = new ServiceController("TabletInputService"); // Display name: "Touch Keyboard and Handwriting Panel Service"
            Logger.Info($"Service check: TabletInputService status={sc.Status}");
            if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.Paused)
            {
                Logger.Info("Service action: starting TabletInputService...");
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                Logger.Info($"Service result: TabletInputService status={sc.Status}");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("Service note: Could not verify/start TabletInputService (might lack rights or service name differs).");
            Logger.Error(ex, "Service diagnostic");
        }
    }

    /// <summary>
    /// Best-effort: sets HKCU\Software\Microsoft\TabletTip\1.7\EnableDesktopModeAutoInvoke = 1
    /// This helps auto-show the touch keyboard in desktop mode when an edit control is focused.
    /// </summary>
    private static void TryEnableDesktopModeAutoInvoke()
    {
        try
        {
            const string keyPath = @"Software\Microsoft\TabletTip\1.7";
            const string valueName = "EnableDesktopModeAutoInvoke";

            using var key = Registry.CurrentUser.CreateSubKey(keyPath, true);
            var current = key?.GetValue(valueName) as int? ?? (key?.GetValue(valueName) as IConvertible)?.ToInt32(null) ?? 0;
            if (current != 1)
            {
                key!.SetValue(valueName, 1, RegistryValueKind.DWord);
                Logger.Info($"Registry: Set {keyPath}\\{valueName} = 1");
            }
            else
            {
                Logger.Info($"Registry: {keyPath}\\{valueName} already 1");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Registry: failed to set EnableDesktopModeAutoInvoke");
        }
    }
}
