using AndyTV.Models;

namespace AndyTV.UI;

public partial class AdHocChannelForm : Form
{
    private readonly List<Channel> allItems;
    private Controls.ChannelFilterListControl picker;

    public Channel SelectedItem { get; private set; }

    public AdHocChannelForm(List<Channel> items)
    {
        allItems = items;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Ad Hoc Channel Selection";
        ClientSize = new Size(1000, 800); // match FavoriteChannelForm
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var layoutPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 1,
            ColumnCount = 1,
        };
        layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        picker = new UI.Controls.ChannelFilterListControl { Dock = DockStyle.Fill };
        // Ad-hoc wants longer list: 2+ chars, up to 1000
        picker.SetChannels(allItems);
        picker.ItemActivated += (_, ch) =>
        {
            SelectedItem = ch;
            Close();
        };

        layoutPanel.Controls.Add(picker, 0, 0);
        Controls.Add(layoutPanel);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        picker.FocusFilter();
    }
}