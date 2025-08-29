using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu;

public class MenuTVChannelHelper(ContextMenuStrip menu)
{
    public List<Channel> Channels { get; private set; } = [];

    public async Task LoadChannels(EventHandler channelClick, string m3uURL)
    {
        MenuHelper.AddHeader(menu, "TOP CHANNELS");

        Channels =
        [
            .. (await M3UService.ParseM3U(m3uURL)).OrderBy(
                c => c.Name,
                StringComparer.OrdinalIgnoreCase
            ),
        ];

        var topUsCategories = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["24/7"] =
            [
                "Breaking Bad",
                "Forensic Files",
                "Frasier",
                "Friends",
                "Game of Thrones",
                "I Love Lucy",
                "Saturday Night Live",
                "Seinfeld",
                "The Office",
                "The Simpsons",
                "The Sopranos",
                "The Wire",
                "Unsolved Mysteries",
            ],

            ["Entertainment"] =
            [
                "A&E",
                "AMC",
                "Bravo",
                "Discovery Channel",
                "Disney Channel",
                "E!",
                "Food Network",
                "FX",
                "FXX",
                "FX Movie Channel",
                "FYI",
                "Hallmark Channel",
                "Hallmark Drama",
                "Hallmark Movies & Mysteries",
                "HGTV",
                "History",
                "IFC",
                "Lifetime",
                "National Geographic",
                "National Geographic Wild",
                "Oxygen",
                "Paramount Network",
                "Smithsonian Channel",
                "Syfy",
                "TBS",
                "TCM",
                "TNT",
                "Travel Channel",
                "TruTV",
                "USA Network",
                "VH1",
                "WE TV",
                "Crime + Investigation",
            ],
            ["Kids"] =
            [
                "Boomerang",
                "Cartoon Network",
                "Disney Junior",
                "Disney XD",
                "Nick Jr",
                "Nickelodeon",
                "Nicktoons",
                "Universal Kids",
            ],
            ["Locals"] =
            [
                "ABC",
                "Antenna TV",
                "CBS",
                "Cozi TV",
                "CW",
                "FOX",
                "Ion Mystery",
                "MeTV",
                "MyNetworkTV",
                "NBC",
                "PBS",
            ],
            ["Movies"] =
            [
                "5StarMax",
                "ActionMax",
                "Cinemax",
                "Epix",
                "Epix 2",
                "Epix Drive-In",
                "Epix Hits",
                "Flix",
                "HBO",
                "HBO 2",
                "HBO Comedy",
                "HBO Family",
                "HBO Signature",
                "HBO Zone",
                "MoreMax",
                "Showtime",
                "Showtime 2",
                "Showtime Extreme",
                "Showtime Showcase",
                "Starz",
                "Starz Cinema",
                "Starz Comedy",
                "Starz Edge",
                "Starz Kids & Family",
                "The Movie Channel",
                "The Movie Channel Xtra",
            ],
            ["Music"] = ["AXS TV", "BET", "CMT", "MTV", "MTV2", "Music Choice"],
            ["News"] =
            [
                "ABC News",
                "Bloomberg",
                "CBS News",
                "CNBC",
                "CNN",
                "CSPAN",
                "CSPAN 2",
                "Fox Business Network",
                "Fox News Channel",
                "HLN",
                "MSNBC",
                "NBC News",
                "Newsmax",
                "OANN",
                "The Weather Channel",
                "Weather Channel",
            ],
            ["Other"] =
            [
                "BBC America",
                "Court TV",
                "GSN",
                "ION",
                "ION Plus",
                "Reelz",
                "Trinity Broadcasting",
            ],
            ["Sports"] =
            [
                "ACC Network",
                "Big Ten Network",
                "BTN",
                "CBS Sports Network",
                "ESPN",
                "ESPN 2",
                "ESPN News",
                "ESPNU",
                "Fox Sports 1",
                "Fox Sports 2",
                "Golf Channel",
                "MLB Network",
                "NBA TV",
                "NFL Network",
                "NFL RedZone",
                "NHL Network",
                "SEC Network",
            ],
        };

        var topUkCategories = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Documentary"] =
            [
                "Crime+Investigation",
                "Discovery History",
                "Discovery Science",
                "Sky History",
            ],
            ["Entertainment"] =
            [
                "Alibi",
                "BBC Alba",
                "Dave",
                "Drama",
                "Eden",
                "Gold",
                "Pick",
                "Quest",
                "Really",
                "Sky Atlantic",
                "Sky Max",
                "Sky Mix",
                "Sky Showcase",
                "Sky Witness",
                "W",
                "Yesterday",
            ],
            ["Kids"] = ["CBBC", "CBeebies", "Cartoonito", "POP", "Tiny Pop"],
            ["Main"] =
            [
                "4seven",
                "5Action",
                "5Select",
                "5Star",
                "5USA",
                "BBC Four",
                "BBC One",
                "BBC Scotland",
                "BBC Three",
                "BBC Two",
                "Channel 4",
                "Channel 5",
                "E4",
                "ITV1",
                "ITV2",
                "ITV3",
                "ITV4",
                "ITVBe",
                "S4C",
                "STV",
                "UTV",
            ],
            ["Movies"] =
            [
                "Film4",
                "Sky Cinema Action",
                "Sky Cinema Animation",
                "Sky Cinema Comedy",
                "Sky Cinema Drama",
                "Sky Cinema Family",
                "Sky Cinema Greats",
                "Sky Cinema Premiere",
                "Sky Cinema Sci-Fi & Horror",
                "Sky Cinema Thriller",
            ],
            ["Music"] = ["4Music", "Kerrang!", "Kiss TV", "The Box"],
            ["News"] =
            [
                "Al Jazeera English",
                "BBC News",
                "Euronews",
                "France 24 English",
                "GB News",
                "Sky News",
                "TalkTV",
            ],
            ["Sports"] =
            [
                "Eurosport 1",
                "Eurosport 2",
                "Premier Sports",
                "Sky Sports Cricket",
                "Sky Sports F1",
                "Sky Sports Football",
                "Sky Sports Golf",
                "Sky Sports Main Event",
                "Sky Sports News",
                "TNT Sports 1",
                "TNT Sports 2",
                "TNT Sports 3",
                "TNT Sports 4",
            ],
        };

        void AddTopChannelsMenu(string rootTitle, Dictionary<string, string[]> categories)
        {
            // Simple word-like match: candidate appears as a whole token (boundary on both sides)
            bool MatchesWordLike(string text, string term)
            {
                if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(term))
                    return false;

                int startIndex = 0;
                while (true)
                {
                    int idx = text.IndexOf(term, startIndex, StringComparison.OrdinalIgnoreCase);
                    if (idx < 0)
                        return false;

                    int leftIdx = idx - 1;
                    int rightIdx = idx + term.Length;

                    bool leftBoundary = idx == 0 || !char.IsLetterOrDigit(text[leftIdx]);
                    bool rightBoundary =
                        rightIdx == text.Length || !char.IsLetterOrDigit(text[rightIdx]);

                    if (leftBoundary && rightBoundary)
                        return true;

                    startIndex = idx + 1;
                }
            }

            var rootItem = new ToolStripMenuItem { Text = rootTitle };

            // Build once per AddTopChannelsMenu call
            var allProperNames = topUsCategories
                .Values.SelectMany(x => x)
                .Concat(topUkCategories.Values.SelectMany(x => x))
                .ToList();

            foreach (
                var category in categories.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            )
            {
                var categoryItem = new ToolStripMenuItem(category.Key);

                foreach (
                    var properName in category.Value.OrderBy(
                        n => n,
                        StringComparer.OrdinalIgnoreCase
                    )
                )
                {
                    // Since roots are long-form, make candidates = {long root, base root}
                    string baseName = null;
                    if (properName.EndsWith(" Network", StringComparison.OrdinalIgnoreCase))
                        baseName = properName[..^" Network".Length].Trim();
                    else if (properName.EndsWith(" Channel", StringComparison.OrdinalIgnoreCase))
                        baseName = properName[..^" Channel".Length].Trim();

                    var candidates = baseName is not null
                        ? [properName, baseName]
                        : new[] { properName };

                    var matches = Channels
                        .Where(c => candidates.Any(cand => MatchesWordLike(c.Name, cand)))
                        .Where(c =>
                        {
                            // If a channel name exactly matches any official proper name,
                            // only include it if it equals one of our candidates (root or base).
                            bool isOfficial = allProperNames.Any(pn =>
                                c.Name.Equals(pn, StringComparison.OrdinalIgnoreCase)
                            );
                            if (!isOfficial)
                                return true;

                            return candidates.Any(cand =>
                                c.Name.Equals(cand, StringComparison.OrdinalIgnoreCase)
                            );
                        })
                        .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (matches.Count == 0)
                        continue;

                    var networkItem = new ToolStripMenuItem(properName);
                    foreach (var ch in matches)
                    {
                        var chItem = new ToolStripMenuItem(ch.Name) { Tag = ch };
                        chItem.Click += channelClick;
                        networkItem.DropDownItems.Add(chItem);
                    }

                    categoryItem.DropDownItems.Add(networkItem);
                }

                if (categoryItem.DropDownItems.Count > 0)
                    rootItem.DropDownItems.Add(categoryItem);
            }

            menu.Items.Add(rootItem);
        }

        AddTopChannelsMenu("US", topUsCategories);
        AddTopChannelsMenu("UK", topUkCategories);

        menu.Items.Add(new ToolStripSeparator());
    }

    public Channel ChannelByUrl(string url)
    {
        return Channels.FirstOrDefault(ch =>
            string.Equals(ch.Url.Trim(), url.Trim(), StringComparison.OrdinalIgnoreCase)
        );
    }
}