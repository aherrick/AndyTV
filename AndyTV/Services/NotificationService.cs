namespace AndyTV.Services;

public class NotificationService(Form parentForm)
{
    public void ShowToast(string message, int durationMs = 3000)
    {
        void Show()
        {
            var baseFont = new Font("Segoe UI", 14, FontStyle.Bold);

            // Measure text to determine appropriate toast size
            var textSize = TextRenderer.MeasureText(message, baseFont);
            const int horizontalPadding = 40; // total extra width
            const int verticalPadding = 20;   // total extra height

            var toastWidth = textSize.Width + horizontalPadding;
            var toastHeight = textSize.Height + verticalPadding;

            var toast = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.White,
                ShowInTaskbar = false,
                TopMost = true,
                Size = new Size(toastWidth, toastHeight),
                Opacity = 0.95,
            };

            var label = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                Font = baseFont,
            };

            toast.Controls.Add(label);

            var formPos = parentForm.PointToScreen(Point.Empty);
            const int margin = 40;
            toast.Location = new Point(
                formPos.X + parentForm.ClientSize.Width - toast.Width - margin,
                formPos.Y + parentForm.ClientSize.Height - toast.Height - margin
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