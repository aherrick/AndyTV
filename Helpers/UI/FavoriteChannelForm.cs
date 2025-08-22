using System.ComponentModel;
using System.Text.Json;
using AndyTV.Models;

namespace AndyTV.Helpers.UI;

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
    public const string FAVORITES_FILE = "favorite_channels.json";

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
        Size = new Size(300, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // Channel search - extended to use available width
        var lblChannels = new Label
        {
            Text = "Search:",
            Location = new Point(12, 12),
            AutoSize = true,
        };

        _channelTextBox = new TextBox
        {
            Location = new Point(12, 32),
            Size = new Size(260, 23), // Extended to fill width minus margins
        };
        _suggestionListBox = new ListBox
        {
            Location = new Point(12, 58),
            Size = new Size(260, 60), // Match textbox width
            Visible = false,
        };

        // Selected channels grid
        var lblSelected = new Label
        {
            Text = "Favorites:",
            Location = new Point(12, 130),
            AutoSize = true,
        };

        _channelsGrid = new DataGridView
        {
            Location = new Point(12, 150),
            Size = new Size(220, 180),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            BorderStyle = BorderStyle.FixedSingle,
        };

        _channelsGrid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "Name",
                Width = 130,
            }
        );

        _channelsGrid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Group",
                HeaderText = "Group",
                Width = 85,
            }
        );

        // Control buttons - positioned right next to the grid
        _upButton = new Button
        {
            Text = "↑",
            Location = new Point(240, 150),
            Size = new Size(32, 32),
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
            FlatStyle = FlatStyle.System,
        };

        _downButton = new Button
        {
            Text = "↓",
            Location = new Point(240, 190),
            Size = new Size(32, 32),
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
            FlatStyle = FlatStyle.System,
        };

        _removeButton = new Button
        {
            Text = "✕",
            Location = new Point(240, 230),
            Size = new Size(32, 32),
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
            FlatStyle = FlatStyle.System,
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
            .Take(6)
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
        if (
            _suggestionListBox.SelectedItem is Channel channel
            && !_selectedChannels.Contains(channel)
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
        string filePath = PathHelper.GetPath(FAVORITES_FILE);
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var savedChannels = JsonSerializer.Deserialize<List<Channel>>(json);

            foreach (var channel in savedChannels)
            {
                _selectedChannels.Add(channel);
            }
        }
    }

    private void SaveFavorites()
    {
        string filePath = PathHelper.GetPath(FAVORITES_FILE);
        string json = JsonSerializer.Serialize(_selectedChannels.ToList());
        File.WriteAllText(filePath, json);
    }

    public List<Channel> SelectedChannels => [.. _selectedChannels];
}