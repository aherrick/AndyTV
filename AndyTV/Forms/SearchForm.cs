using AndyTV.Data.Models;
using AndyTV.Data.Services;

namespace AndyTV;

// Type-to-filter picker over the already-loaded channel list; returns the chosen channel.
sealed class SearchForm : Form
{
    private const int MaxResults = 200;

    private readonly List<Channel> _channels;
    private readonly RecentSearchService _recentService;
    private readonly TextBox _search = new() { Dock = DockStyle.Top };

    // Recent search buttons plus Clear; shown only while the search box is empty.
    private readonly FlowLayoutPanel _recent = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = true,
    };

    private readonly ListBox _results = new() { Dock = DockStyle.Fill, IntegralHeight = false };

    public Channel Selected { get; private set; }

    public SearchForm(List<Channel> channels, RecentSearchService recentService)
    {
        _channels = channels;
        _recentService = recentService;

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

        // Fill added before Top so the search box docks above the recent searches and results.
        Controls.Add(_results);
        Controls.Add(_recent);
        Controls.Add(_search);

        // Pop the on-screen keyboard for remote/touch use; close it with the form.
        Shown += (_, _) => KeyboardHelper.ShowOnScreenKeyboard();
        FormClosed += (_, _) => KeyboardHelper.HideOnScreenKeyboard();

        AddRecentButtons();
        Filter();
        ActiveControl = _search;
    }

    private void AddRecentButtons()
    {
        var terms = _recentService.Get();
        foreach (var term in terms)
        {
            AddRecentButton(term, () => SearchFor(term));
        }
        if (terms.Count > 0)
        {
            AddRecentButton("Clear", ClearRecent);
        }
    }

    private void AddRecentButton(string text, Action onClick)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) => onClick();
        _recent.Controls.Add(button);
    }

    // Accept saves the term and moves it to the front, so a click alone changes nothing.
    // Focus returns to the box (the clicked button just hid) so Enter plays the top match.
    private void SearchFor(string term)
    {
        _search.Text = term;
        _search.Focus();
        _search.SelectionStart = term.Length;
    }

    private void ClearRecent()
    {
        _recentService.Clear();
        foreach (var button in _recent.Controls.OfType<Button>().ToList())
        {
            button.Dispose();
        }
        _recent.Hide();
        _search.Focus();
    }

    private void Filter()
    {
        var term = _search.Text.Trim();
        _recent.Visible = term.Length == 0 && _recent.HasChildren;

        // Empty box shows nothing. Never assign null: that clears DisplayMember.
        // DataSource resets selection to the first (top) match, so Enter plays it.
        _results.DataSource =
            term.Length == 0
                ? []
                : _channels
                    .Where(c => c.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Take(MaxResults)
                    .ToList();
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
            // Only a search that led to a channel is kept, so partial terms like "f", "fo" never fill the history.
            _recentService.Add(_search.Text.Trim());
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
