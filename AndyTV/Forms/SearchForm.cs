using AndyTV.Data.Models;

namespace AndyTV;

// Type-to-filter picker over the already-loaded channel list; returns the chosen channel.
sealed class SearchForm : Form
{
    private const int MaxResults = 200;

    private readonly List<Channel> _channels;
    private readonly TextBox _search = new() { Dock = DockStyle.Top };
    private readonly ListBox _results = new() { Dock = DockStyle.Fill, IntegralHeight = false };

    public Channel Selected { get; private set; }

    public SearchForm(List<Channel> channels)
    {
        _channels = channels;

        Text = "Search Channels";
        Size = new Size(520, 560);
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;

        _results.DisplayMember = nameof(Channel.DisplayName);
        _results.DoubleClick += (_, _) => Accept();
        _search.TextChanged += (_, _) => Filter();
        _search.KeyDown += OnSearchKeyDown;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };

        // Fill added before Top so the search box docks above the results.
        Controls.Add(_results);
        Controls.Add(_search);

        // Pop the on-screen keyboard for remote/touch use; close it with the form.
        Shown += (_, _) => KeyboardHelper.ShowOnScreenKeyboard();
        FormClosed += (_, _) => KeyboardHelper.HideOnScreenKeyboard();

        Filter();
        ActiveControl = _search;
    }

    private void Filter()
    {
        var term = _search.Text.Trim();

        // Empty box shows nothing; user types to filter.
        if (term.Length == 0)
        {
            _results.DataSource = null;
            return;
        }

        var matches = _channels.Where(c =>
            c.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
        );

        // DataSource resets selection to the first (top) match, so Enter plays it.
        _results.DataSource = matches.Take(MaxResults).ToList();
    }

    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            Accept();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Down && _results.Items.Count > 0)
        {
            _results.Focus();
            e.Handled = true;
        }
    }

    private void Accept()
    {
        if (_results.SelectedItem is Channel channel)
        {
            Selected = channel;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
