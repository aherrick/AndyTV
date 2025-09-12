using System;
using System.Diagnostics;
using System.IO;
using AndyTV.Helpers;

namespace AndyTV.Helpers;

public static class KeyboardHelper
{
    private static readonly string[] TabTipPaths =
    {
        @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe",
        @"C:\Program Files (x86)\Common Files\microsoft shared\ink\TabTip.exe",
    };

    public static void ShowOnScreenKeyboard()
    {
        Logger.Info("ShowOnScreenKeyboard: start");

        if (TryStart("tabtip.exe", null, useShellExecute: true, "Launch 'tabtip.exe' via PATH"))
        {
            return;
        }

        foreach (var path in TabTipPaths)
        {
            if (File.Exists(path))
            {
                if (TryStart(path, null, useShellExecute: true, $"Launch TabTip from '{path}'"))
                {
                    return;
                }
            }
            else
            {
                Logger.Warn($"TabTip not found at '{path}'");
            }
        }

        if (
            TryStart(
                "cmd.exe",
                "/c start \"\" \"osk.exe\"",
                useShellExecute: false,
                "Launch 'osk.exe' via cmd"
            )
        )
        {
            return;
        }

        if (!TryStart("osk.exe", null, useShellExecute: true, "Launch 'osk.exe' directly"))
        {
            Logger.Warn("All launch attempts exhausted; on-screen keyboard did not start.");
        }
    }

    private static bool TryStart(
        string fileName,
        string arguments,
        bool useShellExecute,
        string action
    )
    {
        try
        {
            Logger.Info(
                $"{action} (file='{fileName}', args={(string.IsNullOrEmpty(arguments) ? "<none>" : arguments)})"
            );

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = useShellExecute,
                CreateNoWindow = !useShellExecute,
                WindowStyle = useShellExecute
                    ? ProcessWindowStyle.Normal
                    : ProcessWindowStyle.Hidden,
            };

            var p = Process.Start(psi);
            Logger.Info($"{action} succeeded (pid={(p?.Id.ToString() ?? "unknown")})");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(
                ex,
                $"{action} failed (file='{fileName}', args={(string.IsNullOrEmpty(arguments) ? "<none>" : arguments)})"
            );
            return false;
        }
    }
}