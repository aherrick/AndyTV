namespace AndyTV.vNext;

sealed class Toast : Form
{
    private static readonly Font ToastFont = new("Segoe UI", 14, FontStyle.Bold);
    private static Toast _current;

    // Keep the toast from stealing focus from the video.
    protected override bool ShowWithoutActivation => true;

    private Toast(string message, int durationMs)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        Opacity = 0.85;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        Controls.Add(new Label
        {
            Text = message,
            AutoSize = true,
            ForeColor = Color.White,
            Font = ToastFont,
            Padding = new Padding(16, 10, 16, 10),
        });

        var timer = new System.Windows.Forms.Timer { Interval = durationMs };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            Close();
        };
        Shown += (_, _) => timer.Start();
    }

    public static void Notify(Form owner, string message, int durationMs = 2500)
    {
        if (owner.InvokeRequired)
        {
            owner.BeginInvoke(() => Notify(owner, message, durationMs));
            return;
        }

        _current?.Close();

        var toast = new Toast(message, durationMs);
        _current = toast;
        toast.FormClosed += (_, _) =>
        {
            if (_current == toast)
            {
                _current = null;
            }
        };
        toast.Load += (_, _) =>
        {
            var area = Screen.FromControl(owner).WorkingArea;
            toast.Location = new Point(
                area.Right - toast.Width - 40,
                area.Bottom - toast.Height - 40);
        };
        toast.Show(owner);
    }
}
