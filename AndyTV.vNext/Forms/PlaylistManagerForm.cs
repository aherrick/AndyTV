using System.ComponentModel;
using AndyTV.Data.Models;

namespace AndyTV.vNext;

sealed class PlaylistManagerForm : Form
{
    public PlaylistManagerForm(List<Playlist> playlists)
    {
        Text = "Manage Playlists";
        Size = new Size(480, 320);
        StartPosition = FormStartPosition.CenterParent;

        var source = new BindingList<Playlist>(playlists);
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            DataSource = source,
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name",
            DataPropertyName = nameof(Playlist.Name),
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

        var deleteButton = new Button
        {
            Text = "Delete Selected",
            Dock = DockStyle.Bottom,
            Height = 32,
        };
        deleteButton.Click += (_, _) =>
        {
            if (grid.CurrentRow?.DataBoundItem is Playlist p)
            {
                source.Remove(p);
            }
        };

        FormClosing += (_, _) => grid.EndEdit();
        Controls.Add(grid);
        Controls.Add(deleteButton);
    }
}
