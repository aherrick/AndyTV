using AndyTV.Models;
using AndyTV.UI.Controls;

namespace AndyTV.UI;

public partial class AdHocChannelForm : Form
{
    private readonly List<Channel> _allItems;
    private ChannelFilterListControl _picker;

    public Channel SelectedItem { get; private set; }

    public AdHocChannelForm(List<Channel> items)
    {
        _allItems = items ?? [];
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Ad Hoc Channel Selection";
        ClientSize = new Size(1000, 800); // matches Favorites
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 1,
        };

        // The header row height is fixed and matches Favorites exactly
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, StandardPickerFactory.PickerHeight));

        _picker = StandardPickerFactory.Create(_allItems);
        _picker.ItemActivated += (_, ch) =>
        {
            SelectedItem = ch;
            Close();
        };

        main.Controls.Add(_picker, 0, 0);
        Controls.Add(main);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _picker.FocusFilter();
    }
}