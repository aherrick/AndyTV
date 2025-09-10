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
        UpdateChannelListStatus(); // show initial hint
        LoadExistingFavorites();
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.Dpi; // ✅ DPI-aware
        Text = "Favorites Manager";
        ClientSize = new Size(1000, 800);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // ----- Root layout -----
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 7,
        };

        // Columns: left = content, right = vertical buttons
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        // Rows
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0 Filter label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1 Filter textbox
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2 Status label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200)); // 3 Channel list
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4 Favorites label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 5 Favorites grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // 6 Bottom buttons

        // ----- Filter label -----
        var filterLabel = new Label
        {
            Text = "Filter Channels:",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 0, 4),
        };
        mainLayout.Controls.Add(filterLabel, 0, 0);
        mainLayout.SetColumnSpan(filterLabel, 2);

        // ----- Filter textbox -----
        _filterTextBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6) };
        _filterTextBox.TextChanged += FilterTextBox_TextChanged;
        _filterTextBox.GotFocus += (_, __) => UIHelper.ShowOnScreenKeyboard();
        mainLayout.Controls.Add(_filterTextBox, 0, 1);
        mainLayout.SetColumnSpan(_filterTextBox, 2);

        // ----- Status label -----
        _statusLabel = new Label
        {
            Text = $"Type at least {MIN_FILTER_LENGTH} characters to search channels...",
            ForeColor = Color.Gray,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6),
        };
        mainLayout.Controls.Add(_statusLabel, 0, 2);
        mainLayout.SetColumnSpan(_statusLabel, 2);

        // ----- Channel list -----
        _channelListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            DisplayMember = "DisplayName",
            Margin = new Padding(0, 0, 6, 8),
        };
        _channelListBox.DoubleClick += ChannelListBox_DoubleClick;
        mainLayout.Controls.Add(_channelListBox, 0, 3);
        mainLayout.SetColumnSpan(_channelListBox, 2); // spans both columns above favorites label

        // ----- Favorites label -----
        var favoritesLabel = new Label
        {
            Text = "Favorites:",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 0, 4),
        };
        mainLayout.Controls.Add(favoritesLabel, 0, 4);
        mainLayout.SetColumnSpan(favoritesLabel, 2);

        // ----- Favorites grid (left) -----
        _favoritesGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            MultiSelect = false,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
            EditMode = DataGridViewEditMode.EditOnEnter,
            Margin = new Padding(0, 0, 6, 0),
        };
        SetupGridColumns();
        SetupCopyPaste();
        mainLayout.Controls.Add(_favoritesGrid, 0, 5);

        // ----- Right-side vertical button panel (aligned with grid) -----
        var rightButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0),
        };

        _moveUpButton = CreateButton("Up", MoveUpButton_Click);
        _moveDownButton = CreateButton("Down", MoveDownButton_Click);
        _removeButton = CreateButton("Remove", RemoveButton_Click);

        rightButtons.Controls.AddRange([_moveUpButton, _moveDownButton, _removeButton]);
        mainLayout.Controls.Add(rightButtons, 1, 5);

        // ----- Bottom buttons (span) -----
        var bottomButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0),
        };

        _importButton = CreateButton("Import", ImportFavorites);
        _exportButton = CreateButton("Export", ExportFavorites);
        _saveButton = CreateButton("Save", SaveButton_Click);

        bottomButtons.Controls.AddRange([_importButton, _exportButton, _saveButton]);
        mainLayout.Controls.Add(bottomButtons, 0, 6);
        mainLayout.SetColumnSpan(bottomButtons, 2);

        Controls.Add(mainLayout);
    }

    // --- helper: consistent system-style buttons ---
    private Button CreateButton(string text, EventHandler onClick)
    {
        var btn = new Button
        {
            Text = text,
            Width = 100,
            Height = 35,
            AutoSize = false,
            Margin = new Padding(4),
        };
        btn.ApplySystemStyle();
        btn.Click += onClick;
        return btn;
    }

    private void SetupForm()
    {
        _favoritesGrid.DataSource = _favorites;
        _channelListBox.DataSource = _filteredChannels;
        _channelListBox.DisplayMember = nameof(Channel.DisplayName);
    }

    private void LoadExistingFavorites()
    {
        var existingFavorites = ChannelDataService.LoadFavoriteChannels() ?? [];
        _favorites.Clear();
        foreach (var channel in existingFavorites)
            _favorites.Add(channel);
        _baseline = SnapshotString();
    }

    private string SnapshotString()
    {
        return string.Join(
            "|",
            _favorites.Select(f => $"{f.DisplayName}:{f.Url}:{f.MappedName}:{f.Group}:{f.Category}")
        );
    }

    // --- Unsaved changes guard ---
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
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
                    _favoritesGrid.CurrentCell = null;
                    _favoritesGrid.EndEdit();
                    ChannelDataService.SaveFavoriteChannels([.. _favorites]);
                    _baseline = SnapshotString();
                    break;

                case DialogResult.Cancel:
                    e.Cancel = true;
                    return;

                case DialogResult.No:
                    break;
            }
        }

        base.OnFormClosing(e);
    }

    // --- Grid columns / copy-paste ---
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

        _favoritesGrid.CellClick += (sender, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex > 0)
                _favoritesGrid.BeginEdit(true);
        };

        _favoritesGrid.CellBeginEdit += (sender, e) =>
        {
            if (e.ColumnIndex == 0)
                e.Cancel = true; // Name is read-only
        };
    }

    private void CopySelectedCell()
    {
        if (_favoritesGrid.CurrentCell?.Value != null)
        {
            var cellValue = _favoritesGrid.CurrentCell.Value?.ToString() ?? string.Empty;
            Clipboard.SetText(cellValue);
        }
    }

    private void PasteToSelectedCell()
    {
        if (
            _favoritesGrid.CurrentCell != null
            && _favoritesGrid.CurrentCell.ColumnIndex > 0
            && Clipboard.ContainsText()
        )
        {
            var text = Clipboard.GetText();
            _favoritesGrid.CurrentCell.Value = text;
            _favoritesGrid.EndEdit();
        }
    }

    // --- Filtering ---
    private void FilterTextBox_TextChanged(object sender, EventArgs e)
    {
        UpdateFilteredChannels();
    }

    private void UpdateFilteredChannels()
    {
        var filterText = _filterTextBox.Text.Trim();

        _filteredChannels.Clear();

        if (filterText.Length >= MIN_FILTER_LENGTH)
        {
            foreach (
                var ch in _allChannels
                    .Where(c =>
                        c.DisplayName.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                    )
                    .Take(MAX_RESULTS)
            )
            {
                _filteredChannels.Add(ch);
            }
        }

        // refresh list binding
        _channelListBox.DataSource = null;
        _channelListBox.DataSource = _filteredChannels;
        _channelListBox.DisplayMember = "DisplayName";

        UpdateChannelListStatus();
    }

    private void UpdateChannelListStatus()
    {
        var filterText = _filterTextBox.Text.Trim();

        if (filterText.Length < MIN_FILTER_LENGTH)
        {
            _statusLabel.Text =
                $"Type at least {MIN_FILTER_LENGTH} characters to search channels...";
            _statusLabel.ForeColor = Color.Gray;
            return;
        }

        var totalMatches = _allChannels.Count(c =>
            c.DisplayName.Contains(filterText, StringComparison.OrdinalIgnoreCase)
        );

        var displayed = Math.Min(totalMatches, MAX_RESULTS);

        if (totalMatches == 0)
        {
            _statusLabel.Text = "No channels found matching your search";
            _statusLabel.ForeColor = Color.DarkRed;
        }
        else if (totalMatches > MAX_RESULTS)
        {
            _statusLabel.Text =
                $"Showing {displayed} of {totalMatches} matches (refine search to see more)";
            _statusLabel.ForeColor = Color.DarkOrange;
        }
        else
        {
            _statusLabel.Text =
                $"Found {totalMatches} matching channel{(totalMatches == 1 ? "" : "s")}";
            _statusLabel.ForeColor = Color.DarkGreen;
        }
    }

    // --- Interactions ---
    private void ChannelListBox_DoubleClick(object sender, EventArgs e)
    {
        if (_channelListBox.SelectedItem is Channel selected)
            AddToFavorites(selected);
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
            _favorites.RemoveAt(index);
    }

    // --- Import / Export / Save ---
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
                    _favorites.Add(ch);
                _baseline = SnapshotString();
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