using System.Text.RegularExpressions;
using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public partial class MenuTVChannelHelper(ContextMenuStrip menu)
{
    private readonly SynchronizationContext _ui =
        SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

    public List<Channel> Channels { get; private set; } = [];

    // ---- helper to create/wire/add a channel item in one place ----
    private static void AddChannelItem(
        ToolStripMenuItem parent,
        Channel ch,
        EventHandler channelClick,
        string displayText = null
    )
    {
        var item = new ToolStripMenuItem(displayText ?? ch.DisplayName) { Tag = ch };
        item.Click += channelClick;
        parent.DropDownItems.Add(item);
    }

    public async Task LoadAndBuildMenu(EventHandler channelClick, string m3uURL)
    {
        MenuHelper.AddHeader(menu, "TOP CHANNELS");

        var parsed = await Task.Run(() => M3UService.ParseM3U(m3uURL));
        Channels = [.. parsed.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)];

        var usTask = Task.Run(() => BuildTopMenuSync("US", BuildTopUs(), channelClick));
        var ukTask = Task.Run(() => BuildTopMenuSync("UK", BuildTopUk(), channelClick));
        var twentyFourSevenTask = Task.Run(() => Build247("24/7", channelClick));
        var movieVodTask = Task.Run(() => BuildVOD("Movie VOD", channelClick));
        var tvVodTask = Task.Run(() => BuildVOD("TV VOD", channelClick));

        var topItems = await Task.WhenAll(
            usTask,
            ukTask,
            twentyFourSevenTask,
            movieVodTask,
            tvVodTask
        );

        _ui.Post(_ => menu.Items.AddRange([.. topItems.Where(item => item != null)]), null);

        Logger.Info("[CHANNELS] Loaded");
    }

    private ToolStripMenuItem BuildVOD(string groupTitle, EventHandler channelClick)
    {
        var root = new ToolStripMenuItem(groupTitle);

        var channels = Channels
            .Where(ch => string.Equals(ch.Group, groupTitle, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ch => ch.DisplayName, StringComparer.OrdinalIgnoreCase);

        foreach (var ch in channels)
        {
            AddChannelItem(root, ch, channelClick);
        }

        return root.DropDownItems.Count > 0 ? root : null;
    }

    private ToolStripMenuItem Build247(string rootTitle, EventHandler channelClick)
    {
        var root = new ToolStripMenuItem(rootTitle);

        // Clean channel name for grouping and top-level display
        static string CleanBaseName(string name)
        {
            var text = name;
            text = TagsRegex().Replace(text, ""); // remove [VIP], [HD], etc.
            text = TwoFourSevenRegex().Replace(text, ""); // remove 24/7
            text = SeasonShortRegex().Replace(text, ""); // remove S1, S01
            text = SeasonLongRegex().Replace(text, ""); // remove Season 1, Season01
            text = NormalizeSpaceRegex().Replace(text, " ").Trim();
            return text;
        }

        // Extract base name and full season string (if present)
        static (string Base, string Season) ExtractBaseAndSeason(string name)
        {
            var baseName = CleanBaseName(name);
            var seasonMatch = SeasonShortRegex().Match(name);
            if (!seasonMatch.Success)
                seasonMatch = SeasonLongRegex().Match(name);
            return (baseName, seasonMatch.Success ? seasonMatch.Value : null);
        }

        // Group channels by cleaned base name
        var grouped = Channels
            .Where(ch =>
                ch.DisplayName.Contains(rootTitle, StringComparison.OrdinalIgnoreCase)
                // Filter out non-English markers like "(AL)", "(DE)"
                && !MatchTwoParens().IsMatch(ch.DisplayName)
            )
            .Select(ch => new { Channel = ch, Info = ExtractBaseAndSeason(ch.DisplayName) })
            .GroupBy(x => x.Info.Base, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                BaseName = g.Key,
                Items = g.OrderBy(x => x.Info.Season ?? "", StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Channel.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
            })
            .ToList();

        // Build menu: singletons at top level; multiples in a submenu under the base name
        foreach (var group in grouped)
        {
            if (group.Items.Count == 1)
            {
                var ch = group.Items[0].Channel;
                AddChannelItem(root, ch, channelClick, CleanBaseName(ch.DisplayName)); // ensures "24/7" is gone
            }
            else
            {
                var subMenu = new ToolStripMenuItem(group.BaseName);
                foreach (var item in group.Items)
                {
                    var ch = item.Channel;
                    var display = CleanBaseName(ch.DisplayName);
                    if (!string.IsNullOrEmpty(item.Info.Season))
                        display = $"{display} {item.Info.Season}"; // e.g., "Seinfeld Season 2"
                    AddChannelItem(subMenu, ch, channelClick, display);
                }
                root.DropDownItems.Add(subMenu);
            }
        }

        return root;
    }

    private ToolStripMenuItem BuildTopMenuSync(
        string rootTitle,
        Dictionary<string, string[][]> categories,
        EventHandler channelClick
    )
    {
        var rootItem = new ToolStripMenuItem(rootTitle);

        foreach (
            var (catName, entries) in categories.OrderBy(
                k => k.Key,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            var catItem = new ToolStripMenuItem(catName);

            foreach (var entry in entries.OrderBy(e => e[0], StringComparer.OrdinalIgnoreCase))
            {
                var display = entry[0];
                var terms = entry;

                var matches = Channels
                    .Where(ch =>
                        terms.Any(term =>
                            ch.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (matches.Count == 0)
                    continue;

                var parent = new ToolStripMenuItem(display);
                foreach (var ch in matches)
                {
                    AddChannelItem(parent, ch, channelClick);
                }

                catItem.DropDownItems.Add(parent);
            }

            if (catItem.DropDownItems.Count > 0)
                rootItem.DropDownItems.Add(catItem);
        }

        return rootItem.DropDownItems.Count > 0 ? rootItem : null;
    }

    public Channel ChannelByUrl(string url)
    {
        return Channels.FirstOrDefault(ch =>
            string.Equals(ch.Url.Trim(), url.Trim(), StringComparison.OrdinalIgnoreCase)
        );
    }

    // ----------------- US/UK category builders -----------------

    private static Dictionary<string, string[][]> BuildTopUs()
    {
        return new(StringComparer.OrdinalIgnoreCase)
        {
            ["24/7"] =
            [
                ["Breaking Bad"],
                ["Forensic Files"],
                ["Frasier"],
                ["Friends"],
                ["Game of Thrones"],
                ["I Love Lucy"],
                ["Saturday Night Live", "SNL"],
                ["Seinfeld"],
                ["The Office"],
                ["The Simpsons"],
                ["The Sopranos"],
                ["The Wire"],
                ["Unsolved Mysteries"],
            ],
            ["Entertainment"] =
            [
                ["A&E", "AE"],
                ["AMC"],
                ["Bravo"],
                ["Comedy Central"],
                ["Discovery Channel", "Discovery"],
                ["Disney Channel", "Disney"],
                ["E!", "E! Entertainment"],
                ["Food Network", "Food"],
                ["FX"],
                ["FXX"],
                ["FX Movie Channel", "FXM"],
                ["FYI"],
                ["Hallmark Channel", "Hallmark"],
                ["Hallmark Drama"],
                ["Hallmark Movies & Mysteries", "Hallmark Movies and Mysteries", "HMM"],
                ["HGTV"],
                ["History", "History Channel"],
                ["IFC"],
                ["Lifetime"],
                ["National Geographic", "Nat Geo"],
                ["National Geographic Wild", "Nat Geo Wild"],
                ["Oxygen"],
                ["Paramount Network", "Paramount"],
                ["Smithsonian Channel", "Smithsonian"],
                ["Syfy", "Sci-Fi"],
                ["TBS"],
                ["TCM", "Turner Classic Movies"],
                ["TNT"],
                ["Travel Channel"],
                ["TruTV"],
                ["USA Network"],
                ["VH1"],
                ["WE TV"],
                ["Crime + Investigation", "Crime & Investigation", "Crime and Investigation"],
            ],
            ["Kids"] =
            [
                ["Boomerang"],
                ["Cartoon Network", "CN"],
                ["Disney Junior"],
                ["Disney XD"],
                ["Nick Jr", "Nick Junior"],
                ["Nickelodeon", "Nick"],
                ["Nicktoons"],
                ["Universal Kids"],
            ],
            ["Locals"] =
            [
                ["ABC"],
                ["Antenna TV", "Antenna"],
                ["CBS"],
                ["Cozi TV", "Cozi"],
                ["CW"],
                ["FOX"],
                ["Ion Mystery", "ION Mystery", "Ion Plus Mystery"],
                ["MeTV"],
                ["MyNetworkTV", "MyNetwork TV", "MyTV"],
                ["NBC"],
                ["PBS"],
            ],
            ["Movies"] =
            [
                ["5StarMax"],
                ["ActionMax"],
                ["Cinemax"],
                ["Epix", "MGM+"],
                ["Epix 2", "MGM+ 2"],
                ["Epix Drive-In", "MGM+ Drive-In", "MGM+ Drive In"],
                ["Epix Hits", "MGM+ Hits"],
                ["Flix"],
                ["HBO"],
                ["HBO 2"],
                ["HBO Comedy"],
                ["HBO Family"],
                ["HBO Signature"],
                ["HBO Zone"],
                ["MoreMax"],
                ["Showtime"],
                ["Showtime 2"],
                ["Showtime Extreme"],
                ["Showtime Showcase"],
                ["Starz"],
                ["Starz Cinema"],
                ["Starz Comedy"],
                ["Starz Edge"],
                ["Starz Kids & Family", "Starz Kids and Family"],
                ["The Movie Channel", "TMC"],
                ["The Movie Channel Xtra", "TMC Xtra", "TMCXtra"],
            ],
            ["Music"] =
            [
                ["AXS TV", "AXS"],
                ["BET"],
                ["CMT"],
                ["MTV"],
                ["MTV2"],
                ["Music Choice"],
            ],
            ["News"] =
            [
                ["ABC News"],
                ["Bloomberg"],
                ["CBS News"],
                ["CNBC"],
                ["CNN"],
                ["CSPAN", "C-SPAN"],
                ["CSPAN 2", "C-SPAN 2"],
                ["Fox Business", "Fox Business Network"],
                ["Fox News", "Fox News Channel"],
                ["HLN", "Headline News"],
                ["MSNBC"],
                ["NBC News"],
                ["NewsNation"],
                ["Newsmax"],
                ["OANN", "One America News", "One America News Network"],
                ["The Weather Channel", "Weather Channel"],
            ],
            ["Other"] =
            [
                ["BBC America"],
                ["Court TV", "CourtTV"],
                ["GSN", "Game Show Network"],
                ["ION", "ION Television"],
                ["ION Plus", "IonPlus"],
                ["Reelz"],
                ["Trinity Broadcasting", "TBN", "Trinity Broadcasting Network"],
            ],
            ["Sports"] =
            [
                ["ACC Network", "ACCN"],
                ["Big Ten Network", "BTN"],
                ["BTN"],
                ["CBS Sports Network", "CBSSN"],
                ["ESPN"],
                ["ESPN 2", "ESPN2"],
                ["ESPN News", "ESPNews"],
                ["ESPNU"],
                ["Fox Sports 1", "FS1"],
                ["Fox Sports 2", "FS2"],
                ["Golf Channel"],
                ["MLB Network"],
                ["NBA TV", "NBATV"],
                ["NFL Network"],
                ["NFL RedZone", "RedZone"],
                ["NHL Network"],
                ["SEC Network", "SECN"],
            ],
        };
    }

    private static Dictionary<string, string[][]> BuildTopUk()
    {
        return new(StringComparer.OrdinalIgnoreCase)
        {
            ["Documentary"] =
            [
                ["Crime+Investigation", "Crime & Investigation", "Crime and Investigation"],
                ["Discovery History"],
                ["Discovery Science"],
                ["Sky History", "History (UK)"],
            ],
            ["Entertainment"] =
            [
                ["Alibi"],
                ["BBC Alba"],
                ["BritBox"],
                ["Dave"],
                ["Drama"],
                ["Eden"],
                ["Gold"],
                ["Pick"],
                ["Quest"],
                ["Really"],
                ["Sky Atlantic"],
                ["Sky Max"],
                ["Sky Mix"],
                ["Sky Showcase"],
                ["Sky Witness"],
                ["W"],
                ["Yesterday"],
            ],
            ["Kids"] =
            [
                ["CBBC"],
                ["CBeebies"],
                ["Cartoonito"],
                ["POP"],
                ["Tiny Pop"],
            ],
            ["Main"] =
            [
                ["4seven"],
                ["5Action"],
                ["5Select"],
                ["5Star"],
                ["5USA"],
                ["BBC Four"],
                ["BBC One"],
                ["BBC Scotland"],
                ["BBC Three"],
                ["BBC Two"],
                ["Channel 4"],
                ["Channel 5"],
                ["E4"],
                ["ITV1"],
                ["ITV2"],
                ["ITV3"],
                ["ITV4"],
                ["ITVBe"],
                ["S4C"],
                ["STV"],
                ["UTV"],
            ],
            ["Movies"] =
            [
                ["Film4"],
                ["Sky Cinema Action"],
                ["Sky Cinema Animation"],
                ["Sky Cinema Comedy"],
                ["Sky Cinema Drama"],
                ["Sky Cinema Family"],
                ["Sky Cinema Greats"],
                ["Sky Cinema Premiere"],
                ["Sky Cinema Sci-Fi & Horror", "Sky Cinema Sci-Fi and Horror"],
                ["Sky Cinema Thriller"],
            ],
            ["Music"] =
            [
                ["4Music"],
                ["Kerrang!"],
                ["Kiss TV"],
                ["The Box"],
            ],
            ["News"] =
            [
                ["Al Jazeera English", "Al Jazeera"],
                ["BBC News"],
                ["BBC Parliament"],
                ["Euronews"],
                ["France 24 English", "France 24"],
                ["GB News"],
                ["Sky News"],
                ["TalkTV"],
            ],
            ["Sports"] =
            [
                ["Eurosport 1"],
                ["Eurosport 2"],
                ["Premier Sports"],
                ["Sky Sports Cricket"],
                ["Sky Sports F1"],
                ["Sky Sports Football"],
                ["Sky Sports Golf"],
                ["Sky Sports Main Event"],
                ["Sky Sports News"],
                ["TNT Sports 1"],
                ["TNT Sports 2"],
                ["TNT Sports 3"],
                ["TNT Sports 4"],
            ],
        };
    }

    // ----------------- Regex helpers -----------------

    // Matches two-letter codes inside parentheses, e.g., "(AL)", "(DE)".
    [GeneratedRegex(@"\([A-Za-z]{2}\)", RegexOptions.Compiled)]
    private static partial Regex MatchTwoParens();

    // Matches the string "24/7" in any spacing/case form, e.g., "24/7", "24 / 7".
    [GeneratedRegex(@"24\s*/\s*7", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TwoFourSevenRegex();

    // Matches tags wrapped in square brackets, e.g., "[HD]", "[VIP]".
    [GeneratedRegex(@"\[[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TagsRegex();

    // Matches short season codes like "S1", "S01", "S10".
    [GeneratedRegex(@"(?<!\w)S0?\d{1,2}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasonShortRegex();

    // Matches longer season tokens like "Season 1", "Season01".
    [GeneratedRegex(@"Season\s*0?\d{1,2}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasonLongRegex();

    // Matches multiple spaces/tabs/newlines to normalize into a single space.
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex NormalizeSpaceRegex();
}