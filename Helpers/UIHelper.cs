namespace AndyTV.Helpers;

public static class UIHelper
{
    /// <summary>
    /// Apply system/dark-mode styling so the button follows the OS theme.
    /// </summary>
    public static Button ApplySystemStyle(this Button button)
    {
        if (button == null)
            return null!;

        button.FlatStyle = FlatStyle.System;
        button.UseVisualStyleBackColor = true;
        button.BackColor = SystemColors.Control;
        button.ForeColor = SystemColors.ControlText;

        return button; // <-- return self so you can chain it
    }
}