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
}