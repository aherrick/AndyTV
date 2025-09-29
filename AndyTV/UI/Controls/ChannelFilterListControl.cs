using AndyTV.Data.Models;
using AndyTV.Helpers;

namespace AndyTV.UI.Controls;

public static class StandardPickerFactory
{
    public const int PickerHeight = 220; // exact height for the header

    public static ChannelFilterListControl Create(List<Channel> channels)
    {
        var picker = new ChannelFilterListControl
        {
            Dock = DockStyle.Fill,
            Margin = new(0, 0, 0, 8),
        };

        picker.SetChannels(channels);
        return picker;
    }
}

public class ChannelFilterListControl : UserControl
{
    private readonly TextBox _filterTextBox = new()
    {
        Dock = DockStyle.Top,
        Margin = new Padding(0, 0, 0, 6),
        PlaceholderText = "Type at least 3 characters to filter for channel...",
    };

    private readonly ListBox _listBox = new()
    {
        Dock = DockStyle.Fill,
        DisplayMember = nameof(Channel.DisplayName),
    };

    private List<Channel> _all = [];
    private List<Channel> _filtered = [];

    private const int MIN_FILTER_LENGTH = 2;
    private const int MAX_RESULTS = 1000;

    // one-shot flag (per control instance / per form)
    private bool _keyboardShownOnce = false;

    public event EventHandler<Channel> ItemActivated;

    public ChannelFilterListControl()
    {
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(0),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // filter
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // list

        _filterTextBox.TextChanged += (_, __) => ApplyFilter();

        _listBox.DoubleClick += (_, __) => RaiseActivated();
        _listBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                RaiseActivated();
                e.Handled = true;
            }
        };

        layout.Controls.Add(_filterTextBox, 0, 0);
        layout.Controls.Add(_listBox, 0, 1);

        Controls.Add(layout);
    }

    public void SetChannels(List<Channel> all)
    {
        _all = all ?? [];
        ApplyFilter();
    }

    public void FocusFilter()
    {
        if (_keyboardShownOnce)
        {
            return;
        }

        _keyboardShownOnce = true;
        _filterTextBox.Focus();

        Task.Run(KeyboardHelper.ShowOnScreenKeyboard);
    }

    public Channel SelectedItem =>
        (_listBox.SelectedIndex >= 0 && _listBox.SelectedIndex < _filtered.Count)
            ? _filtered[_listBox.SelectedIndex]
            : null;

    private void RaiseActivated()
    {
        var item = SelectedItem;
        if (item is not null)
            ItemActivated?.Invoke(this, item);
    }

    private void ApplyFilter()
    {
        var text = _filterTextBox.Text?.Trim() ?? string.Empty;

        if (text.Length < MIN_FILTER_LENGTH)
        {
            _filtered = [];
        }
        else
        {
            _filtered =
            [
                .. _all.Where(c => c.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .Take(MAX_RESULTS),
            ];
        }

        _listBox.Items.Clear();
        _listBox.Items.AddRange([.. _filtered.Select(c => c.DisplayName)]);
        if (_listBox.Items.Count > 0)
            _listBox.SelectedIndex = 0;
    }
}