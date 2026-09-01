using System.ComponentModel;
using AndyTV.Data.Models;
using FontAwesome.Sharp;

namespace AndyTV.vNext;

sealed class PlaylistManagerForm : Form
{
    public PlaylistManagerForm(List<Playlist> playlists)
    {
        Text = "Manage Playlists";
        Size = new Size(820, 560);
        StartPosition = FormStartPosition.CenterParent;

        var source = new BindingList<Playlist>(playlists);
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
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
        var addButton = IconButtonFactory.Make(
            IconChar.Plus,
            Color.ForestGreen,
            "Add playlist",
            (_, _) =>
            {
                grid.EndEdit();
                // One incomplete playlist at a time.
                if (!source.Any(p => string.IsNullOrWhiteSpace(p.Url)))
                {
                    source.Add(new Playlist { Name = "New Playlist", ShowInMenu = true });
                }
            }
        );
        var deleteButton = IconButtonFactory.Make(
            IconChar.TrashCan,
            Color.Firebrick,
            "Delete playlist",
            (_, _) =>
            {
                grid.EndEdit();
                var i = grid.CurrentRow?.Index ?? -1;
                if (i < 0)
                {
                    return;
                }
                var confirm = MessageBox.Show(
                    this,
                    $"Delete \"{source[i].Name}\"?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (confirm == DialogResult.Yes)
                {
                    source.RemoveAt(i);
                }
            }
        );

        var closeButton = new Button
        {
            Text = "Save & Close",
            UseMnemonic = false,
            AutoSize = true,
            Padding = new Padding(12, 4, 12, 4),
        };
        closeButton.Click += (_, _) => Close();

        var buttonBar = IconButtonFactory.BottomBar(closeButton, addButton, deleteButton);

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
        CancelButton = closeButton;
        Controls.Add(grid);
        Controls.Add(buttonBar);
    }
}
