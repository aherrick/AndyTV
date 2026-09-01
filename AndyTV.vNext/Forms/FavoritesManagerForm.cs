using AndyTV.Data.Models;
using FontAwesome.Sharp;

namespace AndyTV.vNext;

sealed class FavoritesManagerForm : GridManagerForm<Channel>
{
    public FavoritesManagerForm(List<Channel> favorites)
        : base("Manage Favorites", favorites)
    {
        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name",
            DataPropertyName = nameof(Channel.Name),
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Mapped Name",
            DataPropertyName = nameof(Channel.MappedName),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        Grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Group",
            DataPropertyName = nameof(Channel.Group),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });

        var up = IconButtonFactory.Make(
            IconChar.ArrowUp,
            Color.SteelBlue,
            "Move up",
            (_, _) => MoveSelected(-1)
        );
        var down = IconButtonFactory.Make(
            IconChar.ArrowDown,
            Color.SteelBlue,
            "Move down",
            (_, _) => MoveSelected(1)
        );
        var delete = IconButtonFactory.Make(
            IconChar.TrashCan,
            Color.Firebrick,
            "Remove",
            (_, _) =>
            {
                var i = SelectedIndex();
                if (i >= 0)
                {
                    Source.RemoveAt(i);
                }
            }
        );

        Compose(up, down, delete);
    }

    protected override void Normalize()
    {
        // Store cleared overrides as null so DisplayName falls back to the source Name.
        foreach (var fav in Source)
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
    }
}
