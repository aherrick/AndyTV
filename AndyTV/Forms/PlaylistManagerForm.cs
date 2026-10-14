using AndyTV.Data.Models;

namespace AndyTV;

internal sealed class PlaylistManagerForm : GridManagerForm<Playlist>
{
    public PlaylistManagerForm(List<Playlist> playlists)
        : base("Playlists", playlists, p => p.Name)
    {
        Size = new Size(1200, 640);

        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name",
            DataPropertyName = nameof(Playlist.Name),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ToolTipText = "Label shown for this playlist in the channel menu.",
        });
        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "URL / Path",
            DataPropertyName = nameof(Playlist.Url),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ToolTipText = "M3U playlist URL or local file path.",
        });
        Grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Show in Menu",
            DataPropertyName = nameof(Playlist.ShowInMenu),
            ToolTipText = "List this playlist as its own entry in the channel menu.",
        });
        Grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Show in US/UK",
            DataPropertyName = nameof(Playlist.ShowInUsUk),
            ToolTipText = "Include this playlist's channels in the curated US/UK menus.",
        });
        Grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Group A\u2013Z",
            DataPropertyName = nameof(Playlist.GroupByFirstChar),
            ToolTipText = "Group channels under A, B, C… by first letter. Ignored when Smart Group is ticked.",
        });
        Grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Smart Group",
            DataPropertyName = nameof(Playlist.SmartGroup),
            ToolTipText = "Group feeds of the same channel together using the curated US/UK/Global lists (e.g. all Fox News feeds). Events like \"NHL 01:\" group by league; anything else by its first word. Single-channel groups go under Other.",
        });
        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name Find",
            DataPropertyName = nameof(Playlist.NameFind),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ToolTipText = "Regex to find in each channel title (used with Name Replace).",
        });
        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name Replace",
            DataPropertyName = nameof(Playlist.NameReplace),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ToolTipText = "Replacement for Name Find matches; supports $1, ${name}.",
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

        Compose(add);
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
