using System.ComponentModel;
using AndyTV.Data.Models;

namespace AndyTV.vNext;

sealed class PlaylistManagerForm : Form
{
    public PlaylistManagerForm(List<Playlist> playlists)
    {
        Text = "Manage Playlists";
        Size = new Size(640, 360);
        StartPosition = FormStartPosition.CenterParent;

        var source = new BindingList<Playlist>(playlists);
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
            DataPropertyName = nameof(Playlist.Name),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "URL / Path",
            DataPropertyName = nameof(Playlist.Url),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Show",
            DataPropertyName = nameof(Playlist.ShowInMenu),
        });
        grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Group A\u2013Z",
            DataPropertyName = nameof(Playlist.GroupByFirstChar),
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name Find",
            DataPropertyName = nameof(Playlist.NameFind),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name Replace",
            DataPropertyName = nameof(Playlist.NameReplace),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        var deleteColumn = new DataGridViewButtonColumn
        {
            Text = "Delete",
            UseColumnTextForButtonValue = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
        };
        grid.Columns.Add(deleteColumn);

        grid.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && grid.Columns[e.ColumnIndex] == deleteColumn)
            {
                var confirm = MessageBox.Show(
                    this,
                    $"Delete \"{source[e.RowIndex].Name}\"?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirm == DialogResult.Yes)
                {
                    source.RemoveAt(e.RowIndex);
                }
            }
        };

        var addButton = new Button
        {
            Text = "Add Playlist",
            Dock = DockStyle.Bottom,
            Height = 32,
        };
        addButton.Click += (_, _) =>
        {
            grid.EndEdit();
            // One incomplete playlist at a time.
            if (!source.Any(p => string.IsNullOrWhiteSpace(p.Url)))
            {
                source.Add(new Playlist { Name = "New Playlist", ShowInMenu = true });
            }
        };

        FormClosing += (_, _) =>
        {
            grid.EndEdit();
            // Never save a playlist without a URL / path.
            for (var i = source.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(source[i].Url))
                {
                    source.RemoveAt(i);
                }
            }
        };
        Controls.Add(grid);
        Controls.Add(addButton);
    }
}
