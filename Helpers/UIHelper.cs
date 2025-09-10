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
        try
        {
            // Method 1: Direct TabTip call (simplest)
            Process.Start("tabtip.exe");
            return;
        }
        catch
        {
            // Try next method
        }

        try
        {
            // Method 2: Full path to TabTip
            var tabTipPath = @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe";
            if (File.Exists(tabTipPath))
            {
                Process.Start(tabTipPath);
                return;
            }
        }
        catch
        {
            // Try next method
        }

        try
        {
            // Method 3: Use Windows Shell to open touch keyboard
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c start \"\" \"osk.exe\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                }
            );
            return;
        }
        catch
        {
            // Try next method
        }

        try
        {
            // Method 4: Direct OSK as final fallback
            Process.Start("osk.exe");
        }
        catch
        {
            // Nothing worked
        }
    }
}