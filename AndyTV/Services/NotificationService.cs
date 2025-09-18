namespace AndyTV.Services;

public class NotificationService(Form parentForm)
{
    public void ShowToast(string message, int durationMs = 3000)
    {
        void Show()
        {
            var toast = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.White,
                ShowInTaskbar = false,
                TopMost = true,
                Size = new Size(350, 60),
                Opacity = 0.95,
            };

            var label = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
            };

            toast.Controls.Add(label);

            var formPos = parentForm.PointToScreen(Point.Empty);
            toast.Location = new Point(
                formPos.X + parentForm.ClientSize.Width - toast.Width - 40,
                formPos.Y + parentForm.ClientSize.Height - toast.Height - 40
            );

            toast.Shown += async (_, __) =>
            {
                await Task.Delay(durationMs);
                toast.Close();
            };

            toast.Show(parentForm);
        }

        if (parentForm.InvokeRequired)
            parentForm.BeginInvoke(Show);
        else
            Show();
    }
}