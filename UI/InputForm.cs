using AndyTV.Helpers;

namespace AndyTV.UI;

public class InputForm : Form
{
    public TextBox InputBox { get; } = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();

    public string Result => InputBox.Text.Trim();

    public InputForm(string title, string prompt, string defaultText = "")
    {
        AutoScaleMode = AutoScaleMode.Dpi; // ✅ DPI-aware
        InitializeForm(title);
        CreateControls(prompt, defaultText);
    }

    private void InitializeForm(string title)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(400, 140); // Will autosize if needed
    }

    private void CreateControls(string prompt, string defaultText)
    {
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            RowCount = 3,
            ColumnCount = 1,
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Prompt
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // TextBox
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Buttons

        // Prompt label
        var promptLabel = new Label
        {
            Text = prompt,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5),
        };

        // Input textbox
        InputBox.Dock = DockStyle.Fill;
        InputBox.Text = defaultText;

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
        };

        _okButton.Text = "OK";
        _okButton.DialogResult = DialogResult.OK;
        _okButton.ApplySystemStyle();

        _cancelButton.Text = "Cancel";
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.ApplySystemStyle();

        buttonPanel.Controls.AddRange([_cancelButton, _okButton]);

        mainPanel.Controls.Add(promptLabel, 0, 0);
        mainPanel.Controls.Add(InputBox, 0, 1);
        mainPanel.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(mainPanel);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        Shown += (_, _) =>
        {
            InputBox.Focus();
        };
    }
}