using AndyTV.Helpers;

namespace AndyTV.UI;

public class InputForm : Form
{
    private const int FormPadding = 10;
    private const int ButtonWidth = 75;
    private const int ButtonSpacing = 10;
    private const int FormWidth = 400;
    private const int FormHeight = 120;

    public TextBox InputBox { get; } = new TextBox();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();

    public string Result => InputBox.Text.Trim();

    public InputForm(string title, string prompt, string defaultText = "")
    {
        InitializeForm(title);
        CreateControls(prompt, defaultText);
        SetupLayout();
    }

    private void InitializeForm(string title)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(FormWidth, FormHeight);
    }

    private void CreateControls(string prompt, string defaultText)
    {
        var promptLabel = new Label
        {
            Left = FormPadding,
            Top = FormPadding,
            Text = prompt,
            AutoSize = true,
        };

        InputBox.Left = FormPadding;
        InputBox.Top = promptLabel.Bottom + 5;
        InputBox.Width = FormWidth - (FormPadding * 2);
        InputBox.Text = defaultText;

        var buttonTop = InputBox.Bottom + 15;
        var buttonRight = FormWidth - FormPadding;

        _cancelButton.Text = "Cancel";
        _cancelButton.Left = buttonRight - ButtonWidth;
        _cancelButton.Width = ButtonWidth;
        _cancelButton.Top = buttonTop;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.ApplySystemStyle();

        _okButton.Text = "OK";
        _okButton.Left = _cancelButton.Left - ButtonWidth - ButtonSpacing;
        _okButton.Width = ButtonWidth;
        _okButton.Top = buttonTop;
        _okButton.DialogResult = DialogResult.OK;
        _okButton.ApplySystemStyle();

        Controls.AddRange([promptLabel, InputBox, _okButton, _cancelButton]);
    }

    private void SetupLayout()
    {
        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        // Set focus to the input box when form loads
        InputBox.Select();
        InputBox.SelectAll();
    }
}