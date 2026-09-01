using System.ComponentModel;
using AndyTV.Data.Models;
using FontAwesome.Sharp;

namespace AndyTV.vNext;

sealed class FavoritesManagerForm : Form
{
    public FavoritesManagerForm(List<Channel> favorites)
    {
        Text = "Manage Favorites";
        Size = new Size(820, 560);
        StartPosition = FormStartPosition.CenterParent;

        var source = new BindingList<Channel>(favorites);
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
        void Move(int delta)
        {
            grid.EndEdit();
            var i = grid.CurrentRow?.Index ?? -1;
            var j = i + delta;
            if (i < 0 || j < 0 || j >= source.Count)
            {
                return;
            }
            (source[j], source[i]) = (source[i], source[j]);
            grid.CurrentCell = grid.Rows[j].Cells[0];
        }

        var upButton = IconButtonFactory.Make(
            IconChar.ArrowUp,
            Color.SteelBlue,
            "Move up",
            (_, _) => Move(-1)
        );
        var downButton = IconButtonFactory.Make(
            IconChar.ArrowDown,
            Color.SteelBlue,
            "Move down",
            (_, _) => Move(1)
        );
        var deleteButton = IconButtonFactory.Make(
            IconChar.TrashCan,
            Color.Firebrick,
            "Remove",
            (_, _) =>
            {
                grid.EndEdit();
                var i = grid.CurrentRow?.Index ?? -1;
                if (i >= 0)
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

        var buttonBar = IconButtonFactory.BottomBar(closeButton, upButton, downButton, deleteButton);

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
        CancelButton = closeButton;
        Controls.Add(grid);
        Controls.Add(buttonBar);
    }
}
