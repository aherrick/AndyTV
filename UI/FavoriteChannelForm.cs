using System.ComponentModel;
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

    private string _baseline = ""; // captured after load & after save

    private const char US = '\u001F'; // exotic delimiter

    public FavoriteChannelForm(List<Channel> channels)
    {
        _allChannels = channels;
        _selectedChannels = [];

        InitializeUI();
        SetupEvents();
        LoadFavorites(); // sets _baseline

        FormClosing += (sender, e) =>
        {
            if (SnapshotString() == _baseline)
                return;

            var r = MessageBox.Show(
                "You have unsaved changes. Save before closing?",
                "Unsaved changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning
            );

            if (r == DialogResult.Yes)
            {
                SaveButton_Click(sender, EventArgs.Empty);
            }
            else if (r == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        };
    }

    private void InitializeUI()
    {
        Text = "Favorite Channels";
        Size = new Size(860, 650);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;

        // simple layout constants for uniformity
        int pad = 12;
        int sideBtnSize = 34; // ↑ ↓ ✕ buttons
        int sideGap = 8;
        int bottomBtnW = 96,
            bottomBtnH = 32;

        var lblChannels = new Label
        {
            Text = "Search:",
            Location = new Point(pad, pad),
            AutoSize = true,
        };

        _channelTextBox = new TextBox
        {
            Location = new Point(pad, lblChannels.Bottom + 8),
            Size = new Size(Width - 2 * pad - 16, 24), // wide, with a little breathing space
        };

        _suggestionListBox = new ListBox
        {
            Location = new Point(pad, _channelTextBox.Bottom + 8),
            Size = new Size(Width - 2 * pad - 16, 140),
            Visible = false,
        };

        var lblSelected = new Label
        {
            Text = "Favorites:",
            Location = new Point(pad, _suggestionListBox.Bottom + 18),
            AutoSize = true,
        };

        // Grid: narrower so side buttons fit; not anchored to bottom
        _channelsGrid = new DataGridView
        {
            Location = new Point(pad, lblSelected.Bottom + 8),
            Size = new Size(780, 300),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect, // copy cell text
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
            MultiSelect = false,
            AutoGenerateColumns = false,
            BorderStyle = BorderStyle.FixedSingle,
            EditMode = DataGridViewEditMode.EditOnEnter,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, // no Bottom
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };

        // Columns: Name (ro), MappedName (rw), Group (rw), Category (rw)
        var colName = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Channel.Name),
            HeaderText = nameof(Channel.Name),
            ReadOnly = true,
            FillWeight = 28,
        };
        var colMappedName = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Channel.MappedName),
            HeaderText = nameof(Channel.MappedName),
            ReadOnly = false,
            FillWeight = 32,
        };
        var colGroup = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Channel.Group),
            HeaderText = nameof(Channel.Group),
            ReadOnly = false,
            FillWeight = 20,
        };
        var colCategory = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Channel.Category),
            HeaderText = nameof(Channel.Category),
            ReadOnly = false,
            FillWeight = 20,
        };
        _channelsGrid.Columns.AddRange(colName, colMappedName, colGroup, colCategory);

        // Side buttons: uniform sizing & spacing, aligned to grid top
        int sideX = _channelsGrid.Right + pad;
        _upButton = new Button
        {
            Text = "↑",
            Location = new Point(sideX, _channelsGrid.Top),
            Size = new Size(sideBtnSize, sideBtnSize),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            UseVisualStyleBackColor = true,
        };
        _downButton = new Button
        {
            Text = "↓",
            Location = new Point(sideX, _upButton.Bottom + sideGap),
            Size = new Size(sideBtnSize, sideBtnSize),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            UseVisualStyleBackColor = true,
        };
        _removeButton = new Button
        {
            Text = "✕",
            Location = new Point(sideX, _downButton.Bottom + sideGap),
            Size = new Size(sideBtnSize, sideBtnSize),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            UseVisualStyleBackColor = true,
        };

        // Bottom buttons: compute from ClientSize so they sit cleanly
        int bottomY = ClientSize.Height - pad - bottomBtnH;
        _importButton = new Button
        {
            Text = "Import",
            Size = new Size(bottomBtnW, bottomBtnH),
            Location = new Point(pad, bottomY),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            UseVisualStyleBackColor = true,
        };
        _exportButton = new Button
        {
            Text = "Export",
            Size = new Size(bottomBtnW, bottomBtnH),
            Location = new Point(_importButton.Right + pad, bottomY),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            UseVisualStyleBackColor = true,
        };
        _saveButton = new Button
        {
            Text = "Save",
            Size = new Size(bottomBtnW, bottomBtnH),
            Location = new Point(ClientSize.Width - pad - bottomBtnW, bottomY),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            UseVisualStyleBackColor = true,
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

    private void OnSuggestionSelected(object sender, EventArgs e) => AddSelectedChannel();

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

    // --- Import / Export (delegates to service) ---
    private void ImportFavorites(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json",
            Title = "Import Favorite Channels",
            FileName = ChannelDataService.FavoriteChannelsFile,
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var imported = ChannelDataService.ImportFavoriteChannels(ofd.FileName) ?? [];
                _selectedChannels.Clear();
                foreach (var ch in imported)
                {
                    _selectedChannels.Add(ch);
                }
                _baseline = SnapshotString(); // treat freshly imported as clean
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
            FileName = ChannelDataService.FavoriteChannelsFile,
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                ChannelDataService.ExportFavoriteChannels(_selectedChannels, sfd.FileName);
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
        _channelsGrid.CurrentCell = null;
        _binding.EndEdit();
        ChannelDataService.SaveFavoriteChannels([.. _selectedChannels]);
        _baseline = SnapshotString();

        MessageBox.Show(
            "Save successful.",
            "Favorites",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }

    // --- Load favorites and set baseline ---
    private void LoadFavorites()
    {
        var savedChannels = ChannelDataService.LoadFavoriteChannels();
        foreach (var channel in savedChannels)
        {
            _selectedChannels.Add(channel);
        }

        _baseline = SnapshotString();
    }

    // --- Dirty helpers ---
    private string SnapshotString()
    {
        _channelsGrid.CurrentCell = null;
        _binding.EndEdit();

        return string.Join(
            "\n",
            _selectedChannels.Select(ch =>
                string.Concat(
                    ch.Name ?? "",
                    US,
                    ch.Group ?? "",
                    US,
                    (ch.Url ?? "").ToLowerInvariant(),
                    US,
                    ch.MappedName ?? "",
                    US,
                    ch.Category ?? ""
                )
            )
        );
    }
}