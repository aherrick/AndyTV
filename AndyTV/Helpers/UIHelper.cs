namespace AndyTV.Helpers;

public static class UIHelper
{
    public static Button CreateButton(string text, EventHandler onClick = null)
    {
        var btn = new Button
        {
            Text = text,
            Width = 100,
            Height = 35,
            AutoSize = false,
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