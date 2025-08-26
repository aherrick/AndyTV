using System.ComponentModel;
using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.UI;

public class FavoriteChannelForm : Form
{
    private readonly List<Channel> _allChannels;
    private readonly BindingList<Channel> _selectedChannels;
    private TextBox _channelTextBox;
    private ListBox _suggestionListBox;
    private DataGridView _channelsGrid;
    private Button _upButton;
    private Button _downButton;
    private Button _removeButton;

    public FavoriteChannelForm(List<Channel> channels)
    {
        _allChannels = channels;
        _selectedChannels = [];
        InitializeUI();
        SetupEvents();
        LoadFavorites();

        FormClosing += (s, e) => SaveFavorites();
    }

    private void InitializeUI()
    {
        Text = "Favorite Channels";
        // Bigger overall window
        Size = new Size(720, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;

        // Channel search
        var lblChannels = new Label
        {
            Text = "Search:",
            Location = new Point(12, 12),
            AutoSize = true,
        };

        _channelTextBox = new TextBox { Location = new Point(12, 32), Size = new Size(680, 24) };

        _suggestionListBox = new ListBox
        {
            Location = new Point(12, 58),
            Size = new Size(680, 120),
            Visible = false,
        };

        // Selected channels grid
        var lblSelected = new Label
        {
            Text = "Favorites:",
            Location = new Point(12, 186),
            AutoSize = true,
        };

        _channelsGrid = new DataGridView
        {
            Location = new Point(12, 206),
            Size = new Size(640, 300),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = false, // allow editing mapped columns
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            BorderStyle = BorderStyle.FixedSingle,
            EditMode = DataGridViewEditMode.EditOnEnter,
            Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        };

        // Columns
        var colName = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Channel.Name),
            HeaderText = nameof(Channel.Name),
            Width = 180,
            ReadOnly = true,
        };

        var colGroup = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Channel.Group),
            HeaderText = nameof(Channel.Group),
            Width = 130,
            ReadOnly = true,
        };

        var colMappedName = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Channel.MappedName),
            HeaderText = nameof(Channel.MappedName),
            Width = 180,
            ReadOnly = false,
        };

        var colMappedGroup = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Channel.MappedGroup),
            HeaderText = nameof(Channel.MappedGroup),
            Width = 130,
            ReadOnly = false,
        };

        _channelsGrid.Columns.AddRange(colName, colGroup, colMappedName, colMappedGroup);

        // Control buttons on the right of the grid
        _upButton = new Button
        {
            Text = "↑",
            Location = new Point(660, 206),
            Size = new Size(32, 32),
            Font = new Font(Font.FontFamily, 13, FontStyle.Bold),
            FlatStyle = FlatStyle.System,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };

        _downButton = new Button
        {
            Text = "↓",
            Location = new Point(660, 246),
            Size = new Size(32, 32),
            Font = new Font(Font.FontFamily, 13, FontStyle.Bold),
            FlatStyle = FlatStyle.System,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };

        _removeButton = new Button
        {
            Text = "✕",
            Location = new Point(660, 286),
            Size = new Size(32, 32),
            Font = new Font(Font.FontFamily, 13, FontStyle.Bold),
            FlatStyle = FlatStyle.System,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };

        Controls.AddRange(
            [
                lblChannels,
                _channelTextBox,
                _suggestionListBox,
                lblSelected,
                _channelsGrid,
                _upButton,
                _downButton,
                _removeButton,
            ]
        );
    }

    private void SetupEvents()
    {
        _channelTextBox.TextChanged += OnTextChanged;
        _channelTextBox.KeyDown += OnTextBoxKeyDown;
        _suggestionListBox.Click += OnSuggestionSelected;
        _suggestionListBox.KeyDown += OnSuggestionKeyDown;
        _upButton.Click += MoveUp;
        _downButton.Click += MoveDown;
        _removeButton.Click += RemoveChannel;
        _channelsGrid.DataSource = _selectedChannels;
    }

    private void OnTextChanged(object sender, EventArgs e)
    {
        string searchText = _channelTextBox.Text.Trim();

        if (string.IsNullOrEmpty(searchText))
        {
            _suggestionListBox.Visible = false;
            return;
        }

        var matches = _allChannels
            .Where(c => c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        if (matches.Count > 0)
        {
            _suggestionListBox.DataSource = matches;
            _suggestionListBox.DisplayMember = "Name";
            _suggestionListBox.Visible = true;
            _suggestionListBox.BringToFront();
        }
        else
        {
            _suggestionListBox.Visible = false;
        }
    }

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down && _suggestionListBox.Visible)
        {
            _suggestionListBox.Focus();
            if (_suggestionListBox.Items.Count > 0)
                _suggestionListBox.SelectedIndex = 0;
        }
        else if (
            e.KeyCode == Keys.Enter
            && _suggestionListBox.Visible
            && _suggestionListBox.SelectedItem != null
        )
        {
            AddSelectedChannel();
        }
    }

    private void OnSuggestionKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            AddSelectedChannel();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            _suggestionListBox.Visible = false;
            _channelTextBox.Focus();
        }
    }

    private void OnSuggestionSelected(object sender, EventArgs e)
    {
        AddSelectedChannel();
    }

    private void AddSelectedChannel()
    {
        var channel = (Channel)_suggestionListBox.SelectedItem;

        // prevent duplicates by URL
        if (
            !_selectedChannels.Any(c =>
                string.Equals(c.Url, channel.Url, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            _selectedChannels.Add(channel);
            _channelTextBox.Clear();
            _suggestionListBox.Visible = false;
            _channelTextBox.Focus();
        }
    }

    private void MoveUp(object sender, EventArgs e)
    {
        if (_channelsGrid.SelectedRows.Count > 0)
        {
            int index = _channelsGrid.SelectedRows[0].Index;
            if (index > 0)
            {
                var item = _selectedChannels[index];
                _selectedChannels.RemoveAt(index);
                _selectedChannels.Insert(index - 1, item);
                _channelsGrid.Rows[index - 1].Selected = true;
            }
        }
    }

    private void MoveDown(object sender, EventArgs e)
    {
        if (_channelsGrid.SelectedRows.Count > 0)
        {
            int index = _channelsGrid.SelectedRows[0].Index;
            if (index < _selectedChannels.Count - 1)
            {
                var item = _selectedChannels[index];
                _selectedChannels.RemoveAt(index);
                _selectedChannels.Insert(index + 1, item);
                _channelsGrid.Rows[index + 1].Selected = true;
            }
        }
    }

    private void RemoveChannel(object sender, EventArgs e)
    {
        if (_channelsGrid.SelectedRows.Count > 0)
        {
            _selectedChannels.RemoveAt(_channelsGrid.SelectedRows[0].Index);
        }
    }

    private void LoadFavorites()
    {
        var savedChannels = ChannelDataService.LoadFavoriteChannels();
        foreach (var channel in savedChannels)
        {
            _selectedChannels.Add(channel);
        }
    }

    private void SaveFavorites()
    {
        ChannelDataService.SaveFavoriteChannels([.. _selectedChannels]);
    }

    public List<Channel> SelectedChannels => [.. _selectedChannels];
}