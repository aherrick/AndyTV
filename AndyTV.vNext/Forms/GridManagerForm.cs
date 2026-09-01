using System.ComponentModel;
using System.Text.Json;

namespace AndyTV.vNext;

// Shared scaffold for the grid-based manager dialogs (Favorites, Playlists).
abstract class GridManagerForm<T> : Form
{
    protected DataGridView Grid { get; }
    protected BindingList<T> Source { get; }

    // True when the dialog left the list different from how it opened.
    public bool Changed { get; private set; }

    private readonly string _snapshot;

    protected GridManagerForm(string title, List<T> items)
    {
        Text = title;
        Size = new Size(820, 560);
        StartPosition = FormStartPosition.CenterParent;

        Source = new BindingList<T>(items);
        _snapshot = JsonSerializer.Serialize(items);
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

        FormClosing += (_, _) =>
        {
            Grid.EndEdit();
            Normalize();
            Changed = JsonSerializer.Serialize(Source) != _snapshot;
        };
    }

    // Subclass adds its columns, then calls this with its left-side action buttons.
    protected void Compose(params Control[] actions)
    {
        var close = new Button
        {
            Text = "Save && Close",
            AutoSize = true,
            Padding = new Padding(14, 6, 14, 6),
        };
        close.Click += (_, _) => Close();
        CancelButton = close;

        Controls.Add(Grid);
        Controls.Add(IconButtonFactory.BottomBar(close, actions));
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

    // Runs on close, before the caller persists the list.
    protected virtual void Normalize() { }
}
