using AndyTV.Data.Models;

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

        var up = ActionButton("Move Up", (_, _) => MoveSelected(-1));
        var down = ActionButton("Move Down", (_, _) => MoveSelected(1));
        var delete = ActionButton(
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
}
