using System.ComponentModel;
using AndyTV.Data.Models;

namespace AndyTV.vNext;

sealed class FavoritesManagerForm : Form
{
    public FavoritesManagerForm(List<Channel> favorites)
    {
        Text = "Manage Favorites";
        Size = new Size(640, 400);
        StartPosition = FormStartPosition.CenterParent;

        var source = new BindingList<Channel>(favorites);
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            DataSource = source,
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name",
            DataPropertyName = nameof(Channel.Name),
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Mapped Name",
            DataPropertyName = nameof(Channel.MappedName),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Group",
            DataPropertyName = nameof(Channel.Group),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        var upColumn = new DataGridViewButtonColumn
        {
            Text = "\u25B2",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
        };
        var downColumn = new DataGridViewButtonColumn
        {
            Text = "\u25BC",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
        };
        var removeColumn = new DataGridViewButtonColumn
        {
            Text = "Remove",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
        };
        grid.Columns.Add(upColumn);
        grid.Columns.Add(downColumn);
        grid.Columns.Add(removeColumn);

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0)
            {
                return;
            }
            grid.EndEdit();
            var column = grid.Columns[e.ColumnIndex];
            var i = e.RowIndex;
            if (column == upColumn && i > 0)
            {
                (source[i - 1], source[i]) = (source[i], source[i - 1]);
                grid.CurrentCell = grid.Rows[i - 1].Cells[e.ColumnIndex];
            }
            else if (column == downColumn && i < source.Count - 1)
            {
                (source[i + 1], source[i]) = (source[i], source[i + 1]);
                grid.CurrentCell = grid.Rows[i + 1].Cells[e.ColumnIndex];
            }
            else if (column == removeColumn)
            {
                source.RemoveAt(i);
            }
        };

        FormClosing += (_, _) =>
        {
            grid.EndEdit();
            // Store cleared overrides as null so DisplayName falls back to the source Name.
            foreach (var fav in source)
            {
                if (string.IsNullOrWhiteSpace(fav.MappedName))
                {
                    fav.MappedName = null;
                }
                if (string.IsNullOrWhiteSpace(fav.Group))
                {
                    fav.Group = null;
                }
            }
        };
        Controls.Add(grid);
    }
}
