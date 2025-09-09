using System.Diagnostics;
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
        Text = "Ad Hoc Channel Selection";
        ClientSize = new Size(384, 370);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        searchTextBox.SetBounds(12, 12, 360, 20);
        searchTextBox.TextChanged += (_, __) => FilterItems();
        searchTextBox.GotFocus += (_, __) => UIHelper.ShowOnScreenKeyboard();

        resultsListBox.SetBounds(12, 38, 360, 320);
        resultsListBox.DoubleClick += (_, __) => SelectChannel();

        Controls.AddRange([searchTextBox, resultsListBox]);
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
