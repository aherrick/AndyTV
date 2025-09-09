using System.Diagnostics;
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

        // Slightly larger font for better readability
        Font = new Font(Font.FontFamily, Font.Size + 1);

        searchTextBox.SetBounds(12, 12, 360, 23);
        searchTextBox.Font = new Font(Font.FontFamily, Font.Size + 1);
        searchTextBox.TextChanged += (_, __) => FilterItems();
        searchTextBox.GotFocus += (_, __) => ShowOnScreenKeyboard();

        resultsListBox.SetBounds(12, 40, 360, 318);
        resultsListBox.Font = new Font(Font.FontFamily, Font.Size + 1);
        resultsListBox.DoubleClick += (_, __) => SelectChannel();

        Controls.AddRange([searchTextBox, resultsListBox]);
    }

    private static void ShowOnScreenKeyboard()
    {
        try
        {
            // Launch legacy On-Screen Keyboard (works on any Windows machine)
            Process.Start("osk.exe");
        }
        catch
        {
            // ignore if unavailable
        }
    }

    private void FilterItems()
    {
        var searchText = searchTextBox.Text;

        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
        {
            // Show everything until user types at least 2 characters
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
