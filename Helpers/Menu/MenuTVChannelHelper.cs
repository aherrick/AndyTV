namespace AndyTV.Helpers.Menu;

public class MenuTVChannelHelper(ContextMenuStrip menu)
{
    public async Task LoadChannels(EventHandler channelClick, string m3uURL)
    {
        MenuHelper.AddHeader(menu, "TOP CHANNELS");

        var channels = await M3UParser.Parse(m3uURL);

        var topUsCategories = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Locals"] =
            [
                "ABC",
                "CBS",
                "CW",
                "FOX",
                "NBC",
                "PBS",
                "MeTV",
                "Cozi TV",
                "Antenna TV",
                "MyNetworkTV",
                "Ion Mystery",
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
                "NHL Network",
                "SEC Network",
            ],
            ["News"] =
            [
                "ABC News",
                "Bloomberg",
                "CBS News",
                "CNN",
                "CNBC",
                "Fox News",
                "HLN",
                "MSNBC",
                "NBC News",
                "Newsmax",
                "OANN",
                "The Weather Channel",
                "Weather Channel",
            ],

            ["Movies"] =
            [
                "HBO",
                "HBO 2",
                "HBO Family",
                "HBO Signature",
                "HBO Comedy",
                "HBO Zone",
                "Cinemax",
                "MoreMax",
                "ActionMax",
                "ThrillerMax",
                "5StarMax",
                "Showtime",
                "Showtime 2",
                "Showtime Showcase",
                "Showtime Extreme",
                "Starz",
                "Starz Edge",
                "Starz Cinema",
                "Starz Comedy",
                "Starz Kids & Family",
                "Epix",
                "Epix 2",
                "Epix Hits",
                "Epix Drive-In",
                "The Movie Channel",
                "The Movie Channel Xtra",
                "Flix",
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
                "Hallmark Channel",
                "Hallmark Drama",
                "Hallmark Movies & Mysteries",
                "HGTV",
                "History",
                "IFC",
                "Lifetime",
                "Oxygen",
                "Paramount Network",
                "Smithsonian Channel",
                "Syfy",
                "TBS",
                "TNT",
                "Travel Channel",
                "TruTV",
                "USA Network",
                "VH1",
                "WE TV",
                "TCM",
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

            ["Music"] = ["BET", "CMT", "MTV", "MTV2", "Music Choice", "AXS TV"],
            ["Other"] =
            [
                "ION",
                "ION Plus",
                "Trinity Broadcasting",
                "BBC America",
                "GSN",
                "Court TV",
                "Reelz",
            ],
        };

        var topUkCategories = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Main"] =
            [
                "BBC One",
                "BBC Two",
                "BBC Three",
                "BBC Four",
                "ITV1",
                "ITV2",
                "ITV3",
                "ITV4",
                "ITVBe",
                "Channel 4",
                "E4",
                "More4",
                "4seven",
                "Channel 5",
                "5USA",
                "5Star",
                "5Action",
                "5Select",
                "S4C",
                "STV",
                "UTV",
                "BBC Scotland",
            ],
            ["Sports"] =
            [
                "Sky Sports Main Event",
                "Sky Sports Premier League",
                "Sky Sports Football",
                "Sky Sports Cricket",
                "Sky Sports F1",
                "Sky Sports Golf",
                "Sky Sports News",
                "TNT Sports 1",
                "TNT Sports 2",
                "TNT Sports 3",
                "TNT Sports 4",
                "Eurosport 1",
                "Eurosport 2",
                "Premier Sports",
            ],
            ["News"] =
            [
                "BBC News",
                "Sky News",
                "GB News",
                "TalkTV",
                "Euronews",
                "Al Jazeera English",
                "France 24 English",
            ],
            ["Entertainment"] =
            [
                "Sky Atlantic",
                "Sky Max",
                "Sky Showcase",
                "Sky Witness",
                "Dave",
                "W",
                "Gold",
                "Alibi",
                "Drama",
                "Yesterday",
                "Eden",
                "Quest",
                "Really",
                "Challenge",
                "Pick",
                "Sky Mix",
                "BBC Alba",
            ],
            ["Documentary"] =
            [
                "Discovery History",
                "Discovery Science",
                "Sky History",
                "Crime+Investigation",
            ],
            ["Movies"] =
            [
                "Sky Cinema Premiere",
                "Sky Cinema Greats",
                "Sky Cinema Action",
                "Sky Cinema Comedy",
                "Sky Cinema Family",
                "Sky Cinema Thriller",
                "Sky Cinema Drama",
                "Sky Cinema Sci-Fi & Horror",
                "Sky Cinema Animation",
                "Film4",
            ],
            ["Kids"] = ["CBBC", "CBeebies", "Cartoonito", "POP", "Tiny Pop"],
            ["Music"] = ["Kiss TV", "Kerrang!", "The Box", "4Music"],
        };

        // ===== Inline reusable function =====
        void AddTopChannelsMenu(string rootTitle, Dictionary<string, string[]> categories)
        {
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

                    bool leftIsBoundary = idx == 0 || !char.IsLetterOrDigit(text[leftIdx]);
                    bool rightIsBoundary =
                        rightIdx == text.Length || !char.IsLetterOrDigit(text[rightIdx]);

                    if (leftIsBoundary && rightIsBoundary)
                        return true; // full word match

                    startIndex = idx + 1; // keep searching
                }
            }

            var rootItem = new ToolStripMenuItem { Text = rootTitle };

            foreach (var category in categories)
            {
                var categoryItem = new ToolStripMenuItem(category.Key);

                // sort networks within category
                foreach (
                    var properName in category.Value.OrderBy(
                        n => n,
                        StringComparer.OrdinalIgnoreCase
                    )
                )
                {
                    // find all channels whose Name matches the network name with boundary-like spacing
                    var matches = channels
                        .Where(c => MatchesWordLike(c.Name, properName))
                        .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (matches.Count == 0)
                        continue;

                    var networkItem = new ToolStripMenuItem(properName);

                    foreach (var ch in matches)
                    {
                        var chItem = new ToolStripMenuItem(ch.Name) { Tag = ch.Url };
                        chItem.Click += channelClick; // uses item.Text for display, item.Tag for URL
                        networkItem.DropDownItems.Add(chItem);
                    }

                    categoryItem.DropDownItems.Add(networkItem);
                }

                if (categoryItem.DropDownItems.Count > 0)
                    rootItem.DropDownItems.Add(categoryItem);
            }

            menu.Items.Add(rootItem);
        }

        // Build both menus using the same function
        AddTopChannelsMenu("US", topUsCategories);
        AddTopChannelsMenu("UK", topUkCategories);

        menu.Items.Add(new ToolStripSeparator());
    }
}