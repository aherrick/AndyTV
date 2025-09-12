using System.Diagnostics;

namespace AndyTV.Helpers;

public static class UIHelper
{
    public static Button ApplySystemStyle(this Button button)
    {
        button.FlatStyle = FlatStyle.System;
        button.UseVisualStyleBackColor = true;
        return button;
    }

    public static void ShowOnScreenKeyboard()
    {
        Logger.Info("ShowOnScreenKeyboard: invoked");

        try
        {
            Logger.Info("Attempt: launch 'tabtip.exe' via PATH");
            var p = Process.Start("tabtip.exe");
            Logger.Info(
                $"Result: started 'tabtip.exe' via PATH (pid={p?.Id.ToString() ?? "unknown"})"
            );
            return;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failure: starting 'tabtip.exe' via PATH threw an exception");
        }

        try
        {
            // Common 64-bit location
            var tabTipPath = @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe";
            // Also check Wow64 (32-bit on 64-bit OS) just for logging visibility
            var tabTipPathWow64 =
                @"C:\Program Files (x86)\Common Files\microsoft shared\ink\TabTip.exe";

            Logger.Info(
                $"Check: exists? '{tabTipPath}' => {File.Exists(tabTipPath)}; '{tabTipPathWow64}' => {File.Exists(tabTipPathWow64)}"
            );

            var launchPath = File.Exists(tabTipPath)
                ? tabTipPath
                : (File.Exists(tabTipPathWow64) ? tabTipPathWow64 : null);

            if (launchPath is null)
            {
                Logger.Warn(
                    "Skip: TabTip.exe not found in expected locations; cannot launch by full path"
                );
            }
            else
            {
                Logger.Info($"Attempt: launch TabTip from full path '{launchPath}'");
                var p = Process.Start(launchPath);
                Logger.Info(
                    $"Result: started TabTip from full path (pid={p?.Id.ToString() ?? "unknown"})"
                );
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failure: starting TabTip from full path threw an exception");
        }

        try
        {
            Logger.Info(
                "Attempt: launch OSK via cmd.exe (`/c start \"\" \"osk.exe\"`) with UseShellExecute=false"
            );
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c start \"\" \"osk.exe\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            var p = Process.Start(psi);
            Logger.Info(
                $"Result: cmd.exe invoked to start OSK (cmd pid={p?.Id.ToString() ?? "unknown"})"
            );
            return;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failure: launching OSK via cmd.exe threw an exception");
        }

        try
        {
            Logger.Info("Attempt: launch 'osk.exe' directly");
            var p = Process.Start("osk.exe");
            Logger.Info(
                $"Result: started 'osk.exe' directly (pid={p?.Id.ToString() ?? "unknown"})"
            );
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failure: starting 'osk.exe' directly threw an exception");
            Logger.Warn("All launch attempts exhausted; on-screen keyboard did not start.");
        }
    }
}