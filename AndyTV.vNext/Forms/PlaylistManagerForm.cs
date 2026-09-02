using AndyTV.Data.Models;

namespace AndyTV.vNext;

sealed class PlaylistManagerForm : GridManagerForm<Playlist>
{
    public PlaylistManagerForm(List<Playlist> playlists)
        : base("Manage Playlists", playlists)
    {
        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name",
            DataPropertyName = nameof(Playlist.Name),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "URL / Path",
            DataPropertyName = nameof(Playlist.Url),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        Grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Show in Menu",
            DataPropertyName = nameof(Playlist.ShowInMenu),
        });
        Grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Show in US/UK",
            DataPropertyName = nameof(Playlist.ShowInUsUk),
        });
        Grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Group A\u2013Z",
            DataPropertyName = nameof(Playlist.GroupByFirstChar),
        });
        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name Find",
            DataPropertyName = nameof(Playlist.NameFind),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name Replace",
            DataPropertyName = nameof(Playlist.NameReplace),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });

        var add = ActionButton(
            "Add",
            (_, _) =>
            {
                Grid.EndEdit();
                // One incomplete playlist at a time.
                if (!Source.Any(p => string.IsNullOrWhiteSpace(p.Url)))
                {
                    Source.Add(new Playlist { Name = "New Playlist", ShowInMenu = true });
                }
            }
        );
        var delete = ActionButton(
            "Delete",
            (_, _) =>
            {
                var i = SelectedIndex();
                if (i < 0)
                {
                    return;
                }
                var confirm = MessageBox.Show(
                    this,
                    $"Delete \"{Source[i].Name}\"?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                if (confirm == DialogResult.Yes)
                {
                    Source.RemoveAt(i);
                }
            }
        );

        Compose(add, delete);
    }

    // Never keep a playlist without a URL / path.
    protected override void BeforeClose()
    {
        for (var i = Source.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(Source[i].Url))
            {
                Source.RemoveAt(i);
            }
        }
    }
}
