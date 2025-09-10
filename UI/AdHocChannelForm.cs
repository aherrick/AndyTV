using AndyTV.Helpers;
using AndyTV.Models;

namespace AndyTV.UI;

public partial class AdHocChannelForm : Form
{
    private readonly TextBox searchTextBox = new();
    private readonly ListBox resultsListBox = new();
    private readonly List<Channel> allItems;
    private List<Channel> filteredItems;

    public Channel SelectedItem { get; private set; }

    public AdHocChannelForm(List<Channel> items)
    {
        allItems = items;
        InitializeComponent();
        FilterItems();
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Ad Hoc Channel Selection";
        ClientSize = new Size(400, 400);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var layoutPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 2,
            ColumnCount = 1,
        };
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        searchTextBox.Dock = DockStyle.Top;
        searchTextBox.Margin = new Padding(0, 0, 0, 8);
        searchTextBox.TextChanged += (_, __) => FilterItems();
        searchTextBox.GotFocus += (_, __) => UIHelper.ShowOnScreenKeyboard();

        resultsListBox.Dock = DockStyle.Fill;
        resultsListBox.DoubleClick += (_, __) => SelectChannel();

        layoutPanel.Controls.Add(searchTextBox, 0, 0);
        layoutPanel.Controls.Add(resultsListBox, 0, 1);

        Controls.Add(layoutPanel);
    }

    private void FilterItems()
    {
        var searchText = searchTextBox.Text;

        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
        {
            filteredItems = [];
        }
        else
        {
            filteredItems =
            [
                .. allItems
                    .Where(c =>
                        c.DisplayName.Contains(
                            searchText,
                            StringComparison.CurrentCultureIgnoreCase
                        )
                    )
                    .Take(1000),
            ];
        }

        resultsListBox.Items.Clear();
        resultsListBox.Items.AddRange([.. filteredItems.Select(c => c.DisplayName)]);

        if (resultsListBox.Items.Count > 0)
        {
            resultsListBox.SelectedIndex = 0;
        }
    }

    private void SelectChannel()
    {
        if (resultsListBox.SelectedIndex >= 0)
        {
            SelectedItem = filteredItems[resultsListBox.SelectedIndex];
            Close();
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        searchTextBox.Focus();
    }
}