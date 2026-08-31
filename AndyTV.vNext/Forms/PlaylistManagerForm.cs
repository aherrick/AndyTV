using System.ComponentModel;

namespace AndyTV.vNext;

sealed class PlaylistManagerForm : Form
{
    public PlaylistManagerForm(List<PlaylistRef> playlists)
    {
        Text = "Manage Playlists";
        Size = new Size(480, 320);
        StartPosition = FormStartPosition.CenterParent;

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            DataSource = new BindingList<PlaylistRef>(playlists),
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name",
            DataPropertyName = nameof(PlaylistRef.Name),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        });
        grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Show",
            DataPropertyName = nameof(PlaylistRef.Visible),
        });
        grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Group A\u2013Z",
            DataPropertyName = nameof(PlaylistRef.Grouped),
        });

        FormClosing += (_, _) => grid.EndEdit();
        Controls.Add(grid);
    }
}
