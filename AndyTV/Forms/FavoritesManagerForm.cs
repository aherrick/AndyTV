using AndyTV.Data.Models;

namespace AndyTV;

internal sealed class FavoritesManagerForm : GridManagerForm<Channel>
{
    public FavoritesManagerForm(List<Channel> favorites)
        : base("Favorites", favorites, c => c.DisplayName)
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

        Compose();
    }
}
