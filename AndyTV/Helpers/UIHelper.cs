namespace AndyTV.Helpers;

public static class UIHelper
{
    public static Button CreateButton(string text, EventHandler onClick = null)
    {
        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 6, 12, 6), // Give text some breathing room
            Margin = new Padding(4),
            FlatStyle = FlatStyle.System,
            UseVisualStyleBackColor = true,
        };

        if (onClick is not null)
        {
            btn.Click += onClick;
        }

        return btn;
    }
}