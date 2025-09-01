namespace AndyTV.UI;

public class InputForm : Form
{
    public TextBox InputBox { get; } = new TextBox();
    private readonly Button _ok;
    private readonly Button _cancel;

    public string Result => DialogResult == DialogResult.OK ? InputBox.Text.Trim() : string.Empty;

    public InputForm(string title, string prompt, string defaultText = "")
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(400, 120);

        var lbl = new Label
        {
            Left = 10,
            Top = 10,
            Text = prompt,
            AutoSize = true,
        };
        InputBox.Left = 10;
        InputBox.Top = 35;
        InputBox.Width = 370;
        InputBox.Text = defaultText;

        _ok = new Button
        {
            Text = "OK",
            Left = 220,
            Width = 75,
            Top = 70,
            DialogResult = DialogResult.OK,
        };
        _cancel = new Button
        {
            Text = "Cancel",
            Left = 305,
            Width = 75,
            Top = 70,
            DialogResult = DialogResult.Cancel,
        };

        Controls.AddRange([lbl, InputBox, _ok, _cancel]);

        AcceptButton = _ok;
        CancelButton = _cancel;
    }
}