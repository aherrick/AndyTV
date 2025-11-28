using System.ComponentModel;
using System.Text.Json;
using AndyTV.Data.Helpers;
using AndyTV.Data.Models;
using AndyTV.Data.Services;
using AndyTV.Helpers;
using AndyTV.UI.Controls;

namespace AndyTV.UI;

public partial class FavoriteChannelForm : Form
{
    // Data
    private readonly List<Channel> _allChannels;
    private readonly IFavoriteChannelService _favoriteChannelService;

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

    public FavoriteChannelForm(List<Channel> channels, IFavoriteChannelService favoriteChannelService, Channel channelAdd = null)
    {
        _allChannels = channels ?? [];
        _favoriteChannelService = favoriteChannelService;

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
        AutoScaleDimensions = new SizeF(96F, 96F);
        Text = "Favorites Manager";
        ClientSize = new Size(1000, 800);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        MinimumSize = new Size(900, 700);

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
        main.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Rows: header picker (fixed), grid (fill), bottom bar (fixed)
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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
        _favoritesGrid.RowTemplate.Height = 28;
        _favoritesGrid.RowHeadersVisible = false;
        SetupGridColumns();
        SetupCopyPaste();
        main.Controls.Add(_favoritesGrid, 0, 1);

        // --- Right vertical buttons (align with grid) ---
        var rightButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Padding = new Padding(6, 0, 0, 0),
            Margin = new Padding(0),
        };

        _moveUpButton = UIHelper.CreateButton("Up", MoveUpButton_Click);
        _moveDownButton = UIHelper.CreateButton("Down", MoveDownButton_Click);
        _removeButton = UIHelper.CreateButton("Remove", RemoveButton_Click);
        rightButtons.Controls.AddRange([_moveUpButton, _moveDownButton, _removeButton]);

        main.Controls.Add(rightButtons, 1, 1);

        // --- Bottom bar: Import/Export (left) + Save (right) ---
        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var leftButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
            WrapContents = false,
            Padding = new Padding(0),
        };

        _importButton = UIHelper.CreateButton("Import", ImportFavorites);
        _exportButton = UIHelper.CreateButton("Export", ExportFavorites);
        leftButtons.Controls.AddRange([_importButton, _exportButton]);

        _saveButton = UIHelper.CreateButton("Save", SaveButton_Click);
        _saveButton.Font = new Font(_saveButton.Font, FontStyle.Bold);
        _saveButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;

        bottom.Controls.Add(leftButtons, 0, 0);
        bottom.Controls.Add(_saveButton, 1, 0);

        main.Controls.Add(bottom, 0, 2);
        main.SetColumnSpan(bottom, 2);

        Controls.Add(main);
        AcceptButton = _saveButton;
    }

    private void LoadExistingFavorites()
    {
        var existing = _favoriteChannelService.LoadFavoriteChannels();
        _favorites.Clear();
        foreach (var ch in existing)
        {
            _favorites.Add(ch);
        }
        _baseline = JsonHelper.GenerateSnapshot(_favorites);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _channelPicker.FocusFilter();

        GridHelper.ApplyStandardRowHeight(_favoritesGrid);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        var current = JsonHelper.GenerateSnapshot(_favorites);
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
            Width = 220,
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
            Width = 220,
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
        if (_favoritesGrid.CurrentCell?.ColumnIndex > 0 && Clipboard.ContainsText())
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

        _favoriteChannelService.SaveFavoriteChannels([.. _favorites]);
        _baseline = JsonHelper.GenerateSnapshot(_favorites);
        Saved = true;
    }

    private void AddChannel(Channel channel)
    {
        if (channel == null || string.IsNullOrWhiteSpace(channel.Url))
            return;

        // Check if already in current unsaved list OR in saved favorites
        bool isDuplicate = _favorites.Any(f => 
            string.Equals(f.Url?.Trim(), channel.Url?.Trim(), StringComparison.OrdinalIgnoreCase))
            || _favoriteChannelService.IsFavorite(channel);
        
        if (isDuplicate)
        {
            MessageBox.Show(
                $"\"{channel.DisplayName}\" is already in Favorites.",
                "Already Added",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        _favorites.Add(channel);
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
        {
            _favorites.RemoveAt(index);
        }
    }

    // --- Import / Export / Save ---
    private const string FavoriteChannelsFile = "favorite_channels.json";

    private void ImportFavorites(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Favorite Channels",
            FileName = FavoriteChannelsFile,
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var json = File.ReadAllText(ofd.FileName);
                var imported = JsonSerializer.Deserialize<List<Channel>>(json) ?? [];
                _favorites.Clear();
                foreach (var ch in imported)
                {
                    _favorites.Add(ch);
                }

                _baseline = JsonHelper.GenerateSnapshot(_favorites);
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
            FileName = FavoriteChannelsFile,
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            var json = JsonSerializer.Serialize(_favorites.ToList());
            File.WriteAllText(sfd.FileName, json);
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
