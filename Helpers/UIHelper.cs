using System.Diagnostics;

namespace AndyTV.Helpers;

public static class UIHelper
{
    public static Button ApplySystemStyle(this Button button)
    {
        button.FlatStyle = FlatStyle.System;
        button.UseVisualStyleBackColor = true;
        button.BackColor = SystemColors.Control;
        button.ForeColor = SystemColors.ControlText;

        return button;
    }

    public static void ShowOnScreenKeyboard()
    {
        try
        {
            // Try modern touch keyboard (Windows 10/11)
            Process.Start("explorer.exe", "ms-touchkeyboard:");
            return;
        }
        catch
        {
            // ignore and try TabTip
        }

        try
        {
            // Try TabTip (modern handwriting/keyboard service)
            var tabTipPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                @"Microsoft Shared\ink\TabTip.exe"
            );
            Process.Start(tabTipPath);
            return;
        }
        catch
        {
            // ignore and try legacy OSK
        }

        try
        {
            // Final fallback: legacy On-Screen Keyboard
            Process.Start(Path.Combine(Environment.SystemDirectory, "osk.exe"));
        }
        catch
        {
            // Nothing left to try
        }
    }
}
