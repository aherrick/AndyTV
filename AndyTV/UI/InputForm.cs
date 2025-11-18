using AndyTV.Helpers;

namespace AndyTV.UI;

public class InputForm : Form
{
    public TextBox InputBox { get; } = new();
    private Button _okButton;
    private Button _cancelButton;

    public string Result => InputBox.Text.Trim();

    public InputForm(string title, string prompt, string defaultText = "")
    {
        AutoScaleMode = AutoScaleMode.Dpi; // ✅ DPI-aware
        AutoScaleDimensions = new SizeF(96F, 96F);
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
        ClientSize = new Size(420, 150);
        MinimumSize = new Size(420, 170);
    }

    private void CreateControls(string prompt, string defaultText)
    {
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            RowCount = 3,
            ColumnCount = 1,
            AutoSize = true,
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Prompt
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // TextBox
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

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

        // Buttons (via UIHelper)
        _okButton = UIHelper.CreateButton("OK");
        _okButton.DialogResult = DialogResult.OK;

        _cancelButton = UIHelper.CreateButton("Cancel");
        _cancelButton.DialogResult = DialogResult.Cancel;

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        buttonPanel.Controls.AddRange([_cancelButton, _okButton]);

        var buttonRow = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonRow.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 0);
        buttonRow.Controls.Add(buttonPanel, 1, 0);

        // Layout
        mainPanel.Controls.Add(promptLabel, 0, 0);
        mainPanel.Controls.Add(InputBox, 0, 1);
        mainPanel.Controls.Add(buttonRow, 0, 2);

        Controls.Add(mainPanel);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        Shown += (_, _) => InputBox.Focus();
    }
}