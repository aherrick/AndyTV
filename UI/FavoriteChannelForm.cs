using System.ComponentModel;
using AndyTV.Helpers;
using AndyTV.Helpers.Menu;
using AndyTV.Models;
using AndyTV.Services;
using AndyTV.UI.Controls;

namespace AndyTV.UI;

public partial class FavoriteChannelForm : Form
{
    // Data
    private readonly List<Channel> _allChannels;

    private readonly BindingList<Channel> _favorites = [];
    private string _baseline = "";

    // UI
    private ChannelFilterListControl _channelPicker;

    private DataGridView _favoritesGrid;
    private Button _moveUpButton;
    private Button _moveDownButton;
    private Button _removeButton;
    private Button _importButton;
    private Button _exportButton;
    private Button _saveButton;

    public bool Saved { get; private set; }

    public FavoriteChannelForm(List<Channel> channels, Channel channelAdd = null)
    {
        _allChannels = channels ?? [];

        InitializeComponent();

        _favoritesGrid.DataSource = _favorites;
        LoadExistingFavorites();

        if (channelAdd is not null)
        {
            AddChannel(channelAdd);
        }
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Favorites Manager";
        ClientSize = new Size(1000, 800);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // Root layout
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 3,
        };

        // Columns: content + right-side buttons
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        // Rows: header picker (fixed), grid (fill), bottom bar (fixed)
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, StandardPickerFactory.PickerHeight));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        // --- Top: shared picker (exactly the same as AdHoc) ---
        _channelPicker = StandardPickerFactory.Create(_allChannels);
        _channelPicker.ItemActivated += (_, ch) => AddChannel(ch);
        main.Controls.Add(_channelPicker, 0, 0);
        main.SetColumnSpan(_channelPicker, 2);

        // --- Grid (left) ---
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
            Margin = new Padding(0),
        };
        SetupGridColumns();
        SetupCopyPaste();
        main.Controls.Add(_favoritesGrid, 0, 1);

        // --- Right vertical buttons (align with grid) ---
        var rightButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(6, 0, 0, 0),
            Margin = new Padding(0),
        };

        _moveUpButton = CreateButton("Up", MoveUpButton_Click);
        _moveDownButton = CreateButton("Down", MoveDownButton_Click);
        _removeButton = CreateButton("Remove", RemoveButton_Click);
        rightButtons.Controls.AddRange([_moveUpButton, _moveDownButton, _removeButton]);

        main.Controls.Add(rightButtons, 1, 1);

        // --- Bottom bar: Import/Export (left) + Save (right) ---
        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0),
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var leftButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0),
            WrapContents = false,
        };

        _importButton = CreateButton("Import", ImportFavorites);
        _exportButton = CreateButton("Export", ExportFavorites);
        leftButtons.Controls.AddRange([_importButton, _exportButton]);

        _saveButton = CreateButton("Save", SaveButton_Click);
        _saveButton.Width = 110;
        _saveButton.Height = 36;
        _saveButton.Font = new Font(_saveButton.Font, FontStyle.Bold);
        _saveButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;

        bottom.Controls.Add(leftButtons, 0, 0);
        bottom.Controls.Add(_saveButton, 1, 0);

        main.Controls.Add(bottom, 0, 2);
        main.SetColumnSpan(bottom, 2);

        Controls.Add(main);
    }

    // --- helper: consistent system-style buttons ---
    private static Button CreateButton(string text, EventHandler onClick)
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

    private void LoadExistingFavorites()
    {
        var existing = ChannelDataService.LoadFavoriteChannels();
        _favorites.Clear();
        foreach (var ch in existing)
        {
            _favorites.Add(ch);
        }
        _baseline = SnapshotString();
    }

    private string SnapshotString() =>
        string.Join(
            "|",
            _favorites.Select(f => $"{f.DisplayName}:{f.Url}:{f.MappedName}:{f.Group}:{f.Category}")
        );

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _channelPicker.FocusFilter();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        var current = SnapshotString();
        if (current != _baseline)
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
                    SaveNow(); // centralized save
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
            Name = nameof(Channel.Name),
            HeaderText = nameof(Channel.Name),
            DataPropertyName = nameof(Channel.Name),
            ReadOnly = true,
            Width = 200,
        };

        var mappedNameColumn = new DataGridViewTextBoxColumn
        {
            Name = nameof(Channel.MappedName),
            HeaderText = "Mapped Name",
            DataPropertyName = nameof(Channel.MappedName),
            Width = 200,
        };

        var groupColumn = new DataGridViewTextBoxColumn
        {
            Name = nameof(Channel.Group),
            HeaderText = nameof(Channel.Group),
            DataPropertyName = nameof(Channel.Group),
            Width = 200,
        };

        var categoryColumn = new DataGridViewTextBoxColumn
        {
            Name = nameof(Channel.Category),
            HeaderText = nameof(Channel.Category),
            DataPropertyName = nameof(Channel.Category),
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

    // --- Minimal refactor helpers ---

    private void SaveNow()
    {
        _favoritesGrid.CurrentCell = null;
        _favoritesGrid.EndEdit();

        ChannelDataService.SaveFavoriteChannels([.. _favorites]);
        _baseline = SnapshotString();
        Saved = true;
    }

    private void AddChannel(Channel channel)
    {
        if (!MenuFavoriteChannelHelper.IsDuplicateUrlAndNotify(channel))
        {
            _favorites.Add(channel);
        }
    }

    // --- Interactions ---

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
        SaveNow(); // centralized save

        MessageBox.Show(
            "Favorites saved successfully!",
            "Save Complete",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );
    }
}