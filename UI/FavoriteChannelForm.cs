using System.ComponentModel;
using System.Text.Json;
using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.UI;

public class FavoriteChannelForm : Form
{
    private readonly List<Channel> _allChannels;
    private readonly BindingList<Channel> _selectedChannels;

    private readonly BindingSource _binding = [];

    private TextBox _channelTextBox;
    private ListBox _suggestionListBox;
    private DataGridView _channelsGrid;
    private Button _upButton;
    private Button _downButton;
    private Button _removeButton;
    private Button _importButton;
    private Button _exportButton;
    private Button _saveButton;

    public FavoriteChannelForm(List<Channel> channels)
    {
        _allChannels = channels;
        _selectedChannels = [];
        InitializeUI();
        SetupEvents();
        LoadFavorites();
    }

    private void InitializeUI()
    {
        Text = "Favorite Channels";
        Size = new Size(720, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;

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
            ReadOnly = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            BorderStyle = BorderStyle.FixedSingle,
            EditMode = DataGridViewEditMode.EditOnEnter,
            Anchor =
                AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        };

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

        _importButton = new Button
        {
            Text = "Import",
            Location = new Point(12, 520),
            Size = new Size(80, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
        };

        _exportButton = new Button
        {
            Text = "Export",
            Location = new Point(100, 520),
            Size = new Size(80, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
        };

        _saveButton = new Button
        {
            Text = "Save",
            Location = new Point(592, 520),
            Size = new Size(100, 30),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
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
                _importButton,
                _exportButton,
                _saveButton,
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

        // Bind through BindingSource so we can EndEdit() safely.
        _binding.DataSource = _selectedChannels;
        _channelsGrid.DataSource = _binding;

        _importButton.Click += ImportFavorites;
        _exportButton.Click += ExportFavorites;
        _saveButton.Click += SaveButton_Click;
    }

    // --- Search / Suggestions ---
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
            _suggestionListBox.DisplayMember = nameof(Channel.Name);
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
            {
                _suggestionListBox.SelectedIndex = 0;
            }
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

    // --- Move / Remove ---
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

    // --- Import / Export ---
    private void ImportFavorites(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            Title = "Import Favorite Channels",
        };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var json = File.ReadAllText(ofd.FileName);
                var imported = JsonSerializer.Deserialize<List<Channel>>(json);

                _selectedChannels.Clear();
                foreach (var ch in imported)
                {
                    _selectedChannels.Add(ch);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Import failed: {ex.Message}");
            }
        }
    }

    private void ExportFavorites(object sender, EventArgs e)
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            Title = "Export Favorite Channels",
            FileName = "favorites.json",
        };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var json = JsonSerializer.Serialize(_selectedChannels.ToList());
                File.WriteAllText(sfd.FileName, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}");
            }
        }
    }

    // --- Save (commit edits + write file) ---
    private void SaveButton_Click(object sender, EventArgs e)
    {
        _channelsGrid.CurrentCell = null; // flush edit box into cell
        _binding.EndEdit(); // finalize binding layer

        ChannelDataService.SaveFavoriteChannels([.. _selectedChannels]);
    }

    // --- Load / Save favorites ---
    private void LoadFavorites()
    {
        var savedChannels = ChannelDataService.LoadFavoriteChannels();
        foreach (var channel in savedChannels)
        {
            _selectedChannels.Add(channel);
        }
    }

    public List<Channel> SelectedChannels => [.. _selectedChannels];
}