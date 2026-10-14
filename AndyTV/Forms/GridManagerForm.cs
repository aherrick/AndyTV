using System.ComponentModel;
using System.Text.Json;

namespace AndyTV;

// Shared scaffold for the grid-based manager dialogs (Favorites, Playlists).
internal abstract class GridManagerForm<T> : Form
{
    protected DataGridView Grid { get; }
    protected BindingList<T> Source { get; }

    // True when the list differs from how it opened; the caller persists after close.
    public bool Changed { get; private set; }

    private readonly string _baseline;

    protected GridManagerForm(string title, List<T> items)
    {
        Text = title;
        Size = new Size(820, 560);
        StartPosition = FormStartPosition.CenterParent;

        Source = new BindingList<T>(items);
        _baseline = JsonSerializer.Serialize(items);
        Grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            DataSource = Source,
        };
    }

    // Subclass adds its columns, then calls this with its own action buttons (before Import/Export and Move Up/Down).
    protected void Compose(params Control[] actions)
    {
        Control[] all =
        [
            .. actions,
            Separator(),
            ActionButton("Import", (_, _) => Import()),
            ActionButton("Export", (_, _) => Export()),
            Separator(),
            ActionButton("Move Up", (_, _) => MoveSelected(-1)),
            ActionButton("Move Down", (_, _) => MoveSelected(1)),
        ];

        var close = new Button
        {
            Text = "Close",
            AutoSize = true,
            Padding = new Padding(14, 6, 14, 6),
        };
        close.Click += (_, _) => Close();
        CancelButton = close;

        Controls.Add(Grid);
        Controls.Add(BottomBar(close, all));
    }

    // Text action button used by the manager dialogs.
    protected static Button ActionButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(2, 0, 2, 0),
        };
        button.Click += onClick;
        return button;
    }

    // Vertical rule; Top|Bottom anchoring stretches it to the button row height.
    private static Label Separator() =>
        new()
        {
            AutoSize = false,
            Width = 2,
            BorderStyle = BorderStyle.Fixed3D,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom,
            Margin = new Padding(8, 2, 8, 2),
        };

    private string FileName => $"{Text.Replace("Manage ", "AndyTV ")}.json";

    private void Export()
    {
        Grid.EndEdit();
        using var dialog = new SaveFileDialog { Filter = "JSON|*.json", FileName = FileName };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            File.WriteAllText(
                dialog.FileName,
                JsonSerializer.Serialize(Source, new JsonSerializerOptions { WriteIndented = true })
            );
        }
    }

    // Replaces the list; saved on close like any other edit.
    private void Import()
    {
        using var dialog = new OpenFileDialog { Filter = "JSON|*.json", FileName = FileName };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        List<T> items;
        try
        {
            items = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(dialog.FileName)) ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            MessageBox.Show(this, $"Couldn't import: {ex.Message}", "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (MessageBox.Show(
                this,
                $"Replace {Source.Count} item(s) with {items.Count} from the file?",
                "Import",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question
            ) != DialogResult.OK)
        {
            return;
        }

        Grid.EndEdit();
        Source.Clear();
        foreach (var item in items)
        {
            Source.Add(item);
        }
    }

    // Action buttons on the left, Close on the right; auto-height so nothing clips.
    private static TableLayoutPanel BottomBar(Button closeButton, Control[] actions)
    {
        var left = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Left,
        };
        left.Controls.AddRange(actions);

        closeButton.Anchor = AnchorStyles.Right;

        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.Controls.Add(left, 0, 0);
        bar.Controls.Add(closeButton, 1, 0);
        return bar;
    }

    // Cleanup applied before the change check (e.g., drop invalid rows).
    protected virtual void BeforeClose() { }

    // Edits auto-save: on close, flag whether anything changed so the caller persists.
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Grid.EndEdit();
        BeforeClose();
        Changed = JsonSerializer.Serialize(Source) != _baseline;
        base.OnFormClosing(e);
    }

    private void MoveSelected(int delta)
    {
        Grid.EndEdit();
        var i = Grid.CurrentRow?.Index ?? -1;
        var j = i + delta;
        if (i < 0 || j < 0 || j >= Source.Count)
        {
            return;
        }
        (Source[j], Source[i]) = (Source[i], Source[j]);
        Grid.CurrentCell = Grid.Rows[j].Cells[0];
    }

    protected int SelectedIndex()
    {
        Grid.EndEdit();
        return Grid.CurrentRow?.Index ?? -1;
    }
}