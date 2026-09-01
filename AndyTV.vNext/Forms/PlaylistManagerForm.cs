using AndyTV.Data.Models;
using FontAwesome.Sharp;

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
            HeaderText = "Show",
            DataPropertyName = nameof(Playlist.ShowInMenu),
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

        var add = IconButtonFactory.Make(
            IconChar.Plus,
            Color.ForestGreen,
            "Add playlist",
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
        var delete = IconButtonFactory.Make(
            IconChar.TrashCan,
            Color.Firebrick,
            "Delete playlist",
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

    protected override void Normalize()
    {
        // Never save a playlist without a URL / path.
        for (var i = Source.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(Source[i].Url))
            {
                Source.RemoveAt(i);
            }
        }
    }
}
