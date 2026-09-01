using System.ComponentModel;
using System.Text.Json;

namespace AndyTV.vNext;

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

    // Subclass adds its columns, then calls this with its left-side action buttons.
    protected void Compose(params Control[] actions)
    {
        var close = new Button
        {
            Text = "Close",
            AutoSize = true,
            Padding = new Padding(14, 6, 14, 6),
        };
        close.Click += (_, _) => Close();
        CancelButton = close;

        Controls.Add(Grid);
        Controls.Add(IconButtonFactory.BottomBar(close, actions));
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

    protected void MoveSelected(int delta)
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