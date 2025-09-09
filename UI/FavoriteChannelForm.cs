using System.ComponentModel;
using AndyTV.Helpers;
using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.UI;

public partial class FavoriteChannelForm : Form
{
    private readonly List<Channel> _allChannels;
    private readonly List<Channel> _filteredChannels;
    private readonly BindingList<Channel> _favorites;
    private string _baseline;

    private TextBox _filterTextBox;
    private ListBox _channelListBox;
    private DataGridView _favoritesGrid;
    private Button _moveUpButton;
    private Button _moveDownButton;
    private Button _removeButton;
    private Button _importButton;
    private Button _exportButton;
    private Button _saveButton;
    private Label _statusLabel;

    private const int MIN_FILTER_LENGTH = 2;
    private const int MAX_RESULTS = 150;

    public FavoriteChannelForm(List<Channel> channels)
    {
        _allChannels = channels ?? [];
        _filteredChannels = [];
        _favorites = [];
        _baseline = "";

        InitializeComponent();
        SetupForm();
        UpdateChannelListStatus(); // Show initial status instead of loading all channels
        LoadExistingFavorites();
    }

    private void LoadExistingFavorites()
    {
        var existingFavorites = ChannelDataService.LoadFavoriteChannels() ?? [];
        _favorites.Clear();
        foreach (var channel in existingFavorites)
        {
            _favorites.Add(channel);
        }
        _baseline = SnapshotString();
    }

    private string SnapshotString()
    {
        // Create a snapshot string for tracking changes
        return string.Join(
            "|",
            _favorites.Select(f => $"{f.DisplayName}:{f.Url}:{f.MappedName}:{f.Group}:{f.Category}")
        );
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Check for unsaved changes
        var currentSnapshot = SnapshotString();
        if (currentSnapshot != _baseline)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before closing?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question
            );

            switch (result)
            {
                case DialogResult.Yes:
                    // Save changes
                    _favoritesGrid.CurrentCell = null;
                    _favoritesGrid.EndEdit();
                    ChannelDataService.SaveFavoriteChannels([.. _favorites]);
                    _baseline = SnapshotString();
                    break;

                case DialogResult.Cancel:
                    // Cancel closing
                    e.Cancel = true;
                    return;

                case DialogResult.No:
                    // Don't save, just close
                    break;
            }
        }

        base.OnFormClosing(e);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // Form properties - increased size significantly
        Text = "Favorites Manager";
        Size = new Size(1000, 800);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // Filter section
        var filterLabel = new Label
        {
            Text = "Filter Channels:",
            Location = new Point(15, 15),
            Size = new Size(100, 23),
            AutoSize = true,
        };

        _filterTextBox = new TextBox { Location = new Point(15, 40), Size = new Size(840, 23) };
        _filterTextBox.TextChanged += FilterTextBox_TextChanged;

        // Status label to show filtering instructions
        _statusLabel = new Label
        {
            Location = new Point(15, 70),
            Size = new Size(840, 20),
            Text = $"Type at least {MIN_FILTER_LENGTH} characters to search channels...",
            ForeColor = Color.Gray,
            AutoSize = false,
        };

        _channelListBox = new ListBox
        {
            Location = new Point(15, 95),
            Size = new Size(840, 200),
            DisplayMember = "DisplayName",
        };
        _channelListBox.DoubleClick += ChannelListBox_DoubleClick;

        // Favorites grid - larger width
        var favoritesLabel = new Label
        {
            Text = "Favorites:",
            Location = new Point(15, 310),
            Size = new Size(100, 23),
            AutoSize = true,
        };

        _favoritesGrid = new DataGridView
        {
            Location = new Point(15, 335),
            Size = new Size(840, 350),
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            MultiSelect = false,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
            EditMode = DataGridViewEditMode.EditOnEnter,
        };

        SetupGridColumns();
        SetupCopyPaste();

        // Control buttons (right side of grid)
        _moveUpButton = new Button
        {
            Text = "Up",
            Location = new Point(870, 335),
            Size = new Size(100, 35),
        };
        _moveUpButton.ApplySystemStyle();
        _moveUpButton.Click += MoveUpButton_Click;

        _moveDownButton = new Button
        {
            Text = "Down",
            Location = new Point(870, 380),
            Size = new Size(100, 35),
        };
        _moveDownButton.ApplySystemStyle();
        _moveDownButton.Click += MoveDownButton_Click;

        _removeButton = new Button
        {
            Text = "Remove",
            Location = new Point(870, 425),
            Size = new Size(100, 35),
        };
        _removeButton.ApplySystemStyle();
        _removeButton.Click += RemoveButton_Click;

        // Bottom buttons
        _importButton = new Button
        {
            Text = "Import",
            Location = new Point(15, 700),
            Size = new Size(100, 35),
        };
        _importButton.ApplySystemStyle();
        _importButton.Click += ImportFavorites;

        _exportButton = new Button
        {
            Text = "Export",
            Location = new Point(125, 700),
            Size = new Size(100, 35),
        };
        _exportButton.ApplySystemStyle();
        _exportButton.Click += ExportFavorites;

        _saveButton = new Button
        {
            Text = "Save",
            Location = new Point(870, 700),
            Size = new Size(100, 35),
        };
        _saveButton.ApplySystemStyle();
        _saveButton.Click += SaveButton_Click;

        // Add controls to form
        Controls.AddRange(
            [
                filterLabel,
                _filterTextBox,
                _statusLabel,
                _channelListBox,
                favoritesLabel,
                _favoritesGrid,
                _moveUpButton,
                _moveDownButton,
                _removeButton,
                _importButton,
                _exportButton,
                _saveButton,
            ]
        );

        ResumeLayout(false);
        PerformLayout();
    }

    private void SetupForm()
    {
        _favoritesGrid.DataSource = _favorites;
        _channelListBox.DataSource = _filteredChannels;
    }

    private void SetupGridColumns()
    {
        var nameColumn = new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = "Name",
            DataPropertyName = "Name",
            ReadOnly = true,
            Width = 200,
        };

        var mappedNameColumn = new DataGridViewTextBoxColumn
        {
            Name = "MappedName",
            HeaderText = "Mapped Name",
            DataPropertyName = "MappedName",
            Width = 200,
        };

        var groupColumn = new DataGridViewTextBoxColumn
        {
            Name = "Group",
            HeaderText = "Group",
            DataPropertyName = "Group",
            Width = 200,
        };

        var categoryColumn = new DataGridViewTextBoxColumn
        {
            Name = "Category",
            HeaderText = "Category",
            DataPropertyName = "Category",
            Width = 200,
        };

        _favoritesGrid.Columns.AddRange(
            [nameColumn, mappedNameColumn, groupColumn, categoryColumn]
        );
    }

    private void SetupCopyPaste()
    {
        _favoritesGrid.KeyDown += (sender, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedCell();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteToSelectedCell();
                e.Handled = true;
            }
        };

        // Enable single-click editing on editable columns
        _favoritesGrid.CellClick += (sender, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex > 0) // Don't allow editing Name column (index 0)
            {
                _favoritesGrid.BeginEdit(true);
            }
        };

        _favoritesGrid.CellBeginEdit += (sender, e) =>
        {
            // Prevent editing on Name column (index 0)
            if (e.ColumnIndex == 0)
            {
                e.Cancel = true;
            }
        };
    }

    private void CopySelectedCell()
    {
        if (_favoritesGrid.CurrentCell?.Value != null)
        {
            var cellValue = _favoritesGrid.CurrentCell.Value.ToString();
            Clipboard.SetText(cellValue ?? string.Empty);
        }
    }

    private void PasteToSelectedCell()
    {
        if (
            _favoritesGrid.CurrentCell != null
            && _favoritesGrid.CurrentCell.ColumnIndex > 0
            && // Don't allow paste into Name column
            Clipboard.ContainsText()
        )
        {
            var clipboardText = Clipboard.GetText();
            _favoritesGrid.CurrentCell.Value = clipboardText;
            _favoritesGrid.EndEdit();
        }
    }

    private void FilterTextBox_TextChanged(object sender, EventArgs e)
    {
        UpdateFilteredChannels();
    }

    private void UpdateFilteredChannels()
    {
        var filterText = _filterTextBox.Text.Trim();

        _filteredChannels.Clear();

        // Only filter if we have enough characters
        if (filterText.Length >= MIN_FILTER_LENGTH)
        {
            var filtered = _allChannels
                .Where(c => c.DisplayName.Contains(filterText, StringComparison.OrdinalIgnoreCase))
                .Take(MAX_RESULTS); // Limit results to improve performance

            foreach (var channel in filtered)
            {
                _filteredChannels.Add(channel);
            }

            UpdateChannelListStatus();
        }
        else
        {
            UpdateChannelListStatus();
        }

        _channelListBox.DataSource = null;
        _channelListBox.DataSource = _filteredChannels;
        _channelListBox.DisplayMember = "DisplayName";
    }

    private void UpdateChannelListStatus()
    {
        var filterText = _filterTextBox.Text.Trim();

        if (filterText.Length < MIN_FILTER_LENGTH)
        {
            _statusLabel.Text =
                $"Type at least {MIN_FILTER_LENGTH} characters to search channels...";
            _statusLabel.ForeColor = Color.Gray;
        }
        else
        {
            var totalMatches = _allChannels.Count(c =>
                c.DisplayName.Contains(filterText, StringComparison.OrdinalIgnoreCase)
            );

            var displayedCount = Math.Min(totalMatches, MAX_RESULTS);

            if (totalMatches > MAX_RESULTS)
            {
                _statusLabel.Text =
                    $"Showing {displayedCount} of {totalMatches} matches (refine search to see more)";
                _statusLabel.ForeColor = Color.DarkOrange;
            }
            else if (totalMatches > 0)
            {
                _statusLabel.Text =
                    $"Found {totalMatches} matching channel{(totalMatches == 1 ? "" : "s")}";
                _statusLabel.ForeColor = Color.DarkGreen;
            }
            else
            {
                _statusLabel.Text = "No channels found matching your search";
                _statusLabel.ForeColor = Color.DarkRed;
            }
        }
    }

    private void ChannelListBox_DoubleClick(object sender, EventArgs e)
    {
        if (_channelListBox.SelectedItem is Channel selectedChannel)
        {
            AddToFavorites(selectedChannel);
        }
    }

    private void AddToFavorites(Channel channel)
    {
        if (
            _favorites.Any(f =>
                string.Equals(f.Url, channel.Url, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            MessageBox.Show(
                $"Channel '{channel.DisplayName}' is already in favorites.",
                "Duplicate Channel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        _favorites.Add(channel);
    }

    private void MoveUpButton_Click(object sender, EventArgs e)
    {
        if (_favoritesGrid.CurrentRow?.Index is not int index || index <= 0)
            return;

        var item = _favorites[index];
        _favorites.RemoveAt(index);
        _favorites.Insert(index - 1, item);

        _favoritesGrid.CurrentCell = _favoritesGrid.Rows[index - 1].Cells[
            _favoritesGrid.CurrentCell?.ColumnIndex ?? 0
        ];
    }

    private void MoveDownButton_Click(object sender, EventArgs e)
    {
        if (_favoritesGrid.CurrentRow?.Index is not int index || index >= _favorites.Count - 1)
            return;

        var item = _favorites[index];
        _favorites.RemoveAt(index);
        _favorites.Insert(index + 1, item);

        _favoritesGrid.CurrentCell = _favoritesGrid.Rows[index + 1].Cells[
            _favoritesGrid.CurrentCell?.ColumnIndex ?? 0
        ];
    }

    private void RemoveButton_Click(object sender, EventArgs e)
    {
        if (_favoritesGrid.CurrentRow?.Index is int index && index >= 0)
        {
            _favorites.RemoveAt(index);
        }
    }

    // --- Import / Export (delegates to service) ---
    private void ImportFavorites(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Favorite Channels",
            FileName = ChannelDataService.FavoriteChannelsFile,
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var imported = ChannelDataService.ImportFavoriteChannels(ofd.FileName) ?? [];
                _favorites.Clear();
                foreach (var ch in imported)
                {
                    _favorites.Add(ch);
                }
                _baseline = SnapshotString(); // freshly imported = clean
                MessageBox.Show(
                    $"Imported {_favorites.Count} favorite(s).",
                    "Import Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Import failed: {ex.Message}",
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }

    private void ExportFavorites(object sender, EventArgs e)
    {
        if (_favorites.Count == 0)
        {
            MessageBox.Show(
                "No favorites to export.",
                "Export",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Export Favorite Channels",
            FileName = ChannelDataService.FavoriteChannelsFile,
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            ChannelDataService.ExportFavoriteChannels(_favorites, sfd.FileName);
            MessageBox.Show(
                $"Exported {_favorites.Count} favorite(s).",
                "Export Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }

    // --- Save (commit edits + write file) ---
    private void SaveButton_Click(object sender, EventArgs e)
    {
        _favoritesGrid.CurrentCell = null;
        _favoritesGrid.EndEdit();

        ChannelDataService.SaveFavoriteChannels([.. _favorites]);
        _baseline = SnapshotString();

        MessageBox.Show(
            "Favorites saved successfully!",
            "Save Complete",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
}