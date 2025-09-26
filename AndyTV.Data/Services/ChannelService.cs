using System.Text.RegularExpressions;
using AndyTV.Data.Models;

namespace AndyTV.Data.Services;

public partial class ChannelService
{
    public static Dictionary<string, List<ChannelTop>> TopUs()
    {
        return new(StringComparer.OrdinalIgnoreCase)
        {
            ["24/7"] =
            [
                new ChannelTop { Name = "Breaking Bad" },
                new ChannelTop { Name = "Forensic Files" },
                new ChannelTop { Name = "Frasier" },
                new ChannelTop { Name = "Friends" },
                new ChannelTop { Name = "Game of Thrones" },
                new ChannelTop { Name = "I Love Lucy" },
                new ChannelTop { Name = "Saturday Night Live", AltNames = ["SNL"] },
                new ChannelTop { Name = "Seinfeld" },
                new ChannelTop { Name = "The Office" },
                new ChannelTop { Name = "The Simpsons" },
                new ChannelTop { Name = "The Sopranos" },
                new ChannelTop { Name = "The Wire" },
                new ChannelTop { Name = "Unsolved Mysteries" },
            ],

            ["Entertainment"] =
            [
                new ChannelTop { Name = "A&E", AltNames = ["AE"] },
                new ChannelTop { Name = "AMC" },
                new ChannelTop { Name = "Bravo", StreamingTVId = "BRAVO" },
                new ChannelTop { Name = "Comedy Central" },
                new ChannelTop
                {
                    Name = "Discovery Channel",
                    AltNames = ["Discovery"],
                    StreamingTVId = "DSCHD",
                },
                new ChannelTop { Name = "Disney Channel", AltNames = ["Disney"] },
                new ChannelTop
                {
                    Name = "E!",
                    AltNames = ["E! Entertainment"],
                    StreamingTVId = "EHD",
                },
                new ChannelTop
                {
                    Name = "Food Network",
                    AltNames = ["Food"],
                    StreamingTVId = "FOOD",
                },
                new ChannelTop { Name = "FX" },
                new ChannelTop { Name = "FXX" },
                new ChannelTop { Name = "FX Movie Channel", AltNames = ["FXM"] },
                new ChannelTop { Name = "FYI" },
                new ChannelTop { Name = "Hallmark Channel", AltNames = ["Hallmark"] },
                new ChannelTop { Name = "Hallmark Drama" },
                new ChannelTop
                {
                    Name = "Hallmark Movies & Mysteries",
                    AltNames = ["Hallmark Movies and Mysteries", "HMM"],
                },
                new ChannelTop { Name = "HGTV", StreamingTVId = "HGTV" },
                new ChannelTop
                {
                    Name = "History",
                    AltNames = ["History Channel"],
                    StreamingTVId = "HSTRYHD",
                },
                new ChannelTop { Name = "IFC" },
                new ChannelTop { Name = "Lifetime" },
                new ChannelTop
                {
                    Name = "National Geographic",
                    AltNames = ["Nat Geo"],
                    StreamingTVId = "NGC",
                },
                new ChannelTop
                {
                    Name = "National Geographic Wild",
                    AltNames = ["Nat Geo Wild"],
                    StreamingTVId = "NGWIHD",
                },
                new ChannelTop { Name = "Oxygen" },
                new ChannelTop { Name = "Paramount Network", AltNames = ["Paramount"] },
                new ChannelTop
                {
                    Name = "Smithsonian Channel",
                    AltNames = ["Smithsonian"],
                    StreamingTVId = "SMTHHD",
                },
                new ChannelTop { Name = "Syfy", AltNames = ["Sci-Fi"] },
                new ChannelTop { Name = "TBS", StreamingTVId = "TBS" },
                new ChannelTop { Name = "TCM", AltNames = ["Turner Classic Movies"] },
                new ChannelTop { Name = "TNT", StreamingTVId = "TNT" },
                new ChannelTop { Name = "Travel Channel", StreamingTVId = "TRAVHD" },
                new ChannelTop { Name = "TruTV", StreamingTVId = "TRUTV" },
                new ChannelTop { Name = "USA Network", StreamingTVId = "USA" },
                new ChannelTop { Name = "VH1" },
                new ChannelTop { Name = "WE TV" },
                new ChannelTop
                {
                    Name = "Crime + Investigation",
                    AltNames = ["Crime & Investigation", "Crime and Investigation"],
                },
            ],

            ["Kids"] =
            [
                new ChannelTop { Name = "Boomerang" },
                new ChannelTop { Name = "Cartoon Network", AltNames = ["CN"] },
                new ChannelTop { Name = "Disney Junior" },
                new ChannelTop { Name = "Disney XD" },
                new ChannelTop { Name = "Nick Jr", AltNames = ["Nick Junior"] },
                new ChannelTop { Name = "Nickelodeon", AltNames = ["Nick"] },
                new ChannelTop { Name = "Nicktoons" },
                new ChannelTop { Name = "Universal Kids" },
            ],

            ["Locals"] =
            [
                new ChannelTop { Name = "ABC" },
                new ChannelTop { Name = "Antenna TV", AltNames = ["Antenna"] },
                new ChannelTop { Name = "CBS" },
                new ChannelTop { Name = "Cozi TV", AltNames = ["Cozi"] },
                new ChannelTop { Name = "CW" },
                new ChannelTop { Name = "FOX" },
                new ChannelTop
                {
                    Name = "Ion Mystery",
                    AltNames = ["ION Mystery", "Ion Plus Mystery"],
                },
                new ChannelTop { Name = "MeTV" },
                new ChannelTop { Name = "MyNetworkTV", AltNames = ["MyNetwork TV", "MyTV"] },
                new ChannelTop { Name = "NBC" },
                new ChannelTop { Name = "PBS" },
            ],

            ["Movies"] =
            [
                new ChannelTop { Name = "5StarMax", StreamingTVId = "MAX" },
                new ChannelTop { Name = "ActionMax", StreamingTVId = "MAX" },
                new ChannelTop { Name = "Cinemax", StreamingTVId = "MAX" },
                new ChannelTop { Name = "Epix", AltNames = ["MGM+"] },
                new ChannelTop { Name = "Epix 2", AltNames = ["MGM+ 2"] },
                new ChannelTop
                {
                    Name = "Epix Drive-In",
                    AltNames = ["MGM+ Drive-In", "MGM+ Drive In"],
                },
                new ChannelTop { Name = "Epix Hits", AltNames = ["MGM+ Hits"] },
                new ChannelTop { Name = "Flix" },
                new ChannelTop { Name = "HBO", StreamingTVId = "HBO" },
                new ChannelTop { Name = "HBO 2", StreamingTVId = "HBO2" },
                new ChannelTop { Name = "HBO Comedy" },
                new ChannelTop { Name = "HBO Family" },
                new ChannelTop { Name = "HBO Signature", StreamingTVId = "HBOSGHD" },
                new ChannelTop { Name = "HBO Zone", StreamingTVId = "HBOZHD" },
                new ChannelTop { Name = "MoreMax", StreamingTVId = "MAX" },
                new ChannelTop { Name = "Showtime" },
                new ChannelTop { Name = "Showtime 2" },
                new ChannelTop { Name = "Showtime Extreme" },
                new ChannelTop { Name = "Showtime Showcase" },
                new ChannelTop { Name = "Starz", StreamingTVId = "STARZ" },
                new ChannelTop { Name = "Starz Cinema" },
                new ChannelTop { Name = "Starz Comedy" },
                new ChannelTop { Name = "Starz Edge" },
                new ChannelTop
                {
                    Name = "Starz Kids & Family",
                    AltNames = ["Starz Kids and Family"],
                },
                new ChannelTop
                {
                    Name = "The Movie Channel",
                    AltNames = ["TMC"],
                    StreamingTVId = "TMC",
                },
                new ChannelTop
                {
                    Name = "The Movie Channel Xtra",
                    AltNames = ["TMC Xtra", "TMCXtra"],
                    StreamingTVId = "TMC",
                },
            ],

            ["Music"] =
            [
                new ChannelTop { Name = "AXS TV", AltNames = ["AXS"] },
                new ChannelTop { Name = "BET" },
                new ChannelTop { Name = "CMT" },
                new ChannelTop { Name = "MTV" },
                new ChannelTop { Name = "MTV2" },
                new ChannelTop { Name = "Music Choice" },
            ],

            ["News"] =
            [
                new ChannelTop { Name = "ABC News" },
                new ChannelTop { Name = "Bloomberg" },
                new ChannelTop { Name = "CBS News" },
                new ChannelTop { Name = "CNBC", StreamingTVId = "CNBC" },
                new ChannelTop { Name = "CNN", StreamingTVId = "CNNHD" },
                new ChannelTop { Name = "CSPAN", AltNames = ["C-SPAN"] },
                new ChannelTop { Name = "CSPAN 2", AltNames = ["C-SPAN 2"] },
                new ChannelTop
                {
                    Name = "Fox Business",
                    AltNames = ["Fox Business Network"],
                    StreamingTVId = "FBN",
                },
                new ChannelTop
                {
                    Name = "Fox News",
                    AltNames = ["Fox News Channel"],
                    StreamingTVId = "FNCHD",
                },
                new ChannelTop { Name = "HLN", AltNames = ["Headline News"] },
                new ChannelTop { Name = "MSNBC", StreamingTVId = "MSNBC" },
                new ChannelTop { Name = "NBC News" },
                new ChannelTop { Name = "NewsNation" },
                new ChannelTop { Name = "Newsmax", StreamingTVId = "NEWSMXH" },
                new ChannelTop
                {
                    Name = "OANN",
                    AltNames = ["One America News", "One America News Network"],
                },
                new ChannelTop { Name = "The Weather Channel", AltNames = ["Weather Channel"] },
            ],

            ["Other"] =
            [
                new ChannelTop { Name = "BBC America", StreamingTVId = "BBCA" },
                new ChannelTop { Name = "Court TV", AltNames = ["CourtTV"] },
                new ChannelTop { Name = "GSN", AltNames = ["Game Show Network"] },
                new ChannelTop { Name = "ION", AltNames = ["ION Television"] },
                new ChannelTop { Name = "ION Plus", AltNames = ["IonPlus"] },
                new ChannelTop { Name = "Reelz" },
                new ChannelTop
                {
                    Name = "Trinity Broadcasting",
                    AltNames = ["TBN", "Trinity Broadcasting Network"],
                },
            ],

            ["Sports"] =
            [
                new ChannelTop
                {
                    Name = "ACC Network",
                    AltNames = ["ACCN"],
                    StreamingTVId = "ACC",
                },
                new ChannelTop
                {
                    Name = "Big Ten Network",
                    AltNames = ["BTN"],
                    StreamingTVId = "BIGTEN",
                },
                new ChannelTop
                {
                    Name = "CBS Sports Network",
                    AltNames = ["CBSSN"],
                    StreamingTVId = "CBSSN",
                },
                new ChannelTop { Name = "ESPN", StreamingTVId = "ESPN" },
                new ChannelTop
                {
                    Name = "ESPN 2",
                    AltNames = ["ESPN2"],
                    StreamingTVId = "ESPN2",
                },
                new ChannelTop
                {
                    Name = "ESPN News",
                    AltNames = ["ESPNews"],
                    StreamingTVId = "ESPNEWS",
                },
                new ChannelTop { Name = "ESPNU", StreamingTVId = "ESPNU" },
                new ChannelTop
                {
                    Name = "Fox Sports 1",
                    AltNames = ["FS1"],
                    StreamingTVId = "FS1",
                },
                new ChannelTop
                {
                    Name = "Fox Sports 2",
                    AltNames = ["FS2"],
                    StreamingTVId = "FS2",
                },
                new ChannelTop { Name = "Golf Channel", StreamingTVId = "GOLF" },
                new ChannelTop { Name = "MLB Network", StreamingTVId = "MLBHD" },
                new ChannelTop
                {
                    Name = "NBA TV",
                    AltNames = ["NBATV"],
                    StreamingTVId = "NBATVHD",
                },
                new ChannelTop { Name = "NFL Network", StreamingTVId = "NFLNET" },
                new ChannelTop { Name = "NFL RedZone", AltNames = ["RedZone"] },
                new ChannelTop { Name = "NHL Network" },
                new ChannelTop
                {
                    Name = "SEC Network",
                    AltNames = ["SECN"],
                    StreamingTVId = "SEC",
                },
            ],
        };
    }

    public static Dictionary<string, List<ChannelTop>> TopUsGuide()
    {
        var source = TopUs();
        var result = new Dictionary<string, List<ChannelTop>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in source)
        {
            var filtered = new List<ChannelTop>();

            foreach (var c in kvp.Value)
            {
                if (!string.IsNullOrEmpty(c.StreamingTVId))
                {
                    filtered.Add(c);
                }
            }

            if (filtered.Count > 0)
            {
                result[kvp.Key] = filtered;
            }
        }

        return result;
    }

    public static Dictionary<string, List<ChannelTop>> TopUk()
    {
        return new(StringComparer.OrdinalIgnoreCase)
        {
            ["Documentary"] =
            [
                new ChannelTop
                {
                    Name = "Crime+Investigation",
                    AltNames = ["Crime & Investigation", "Crime and Investigation"],
                },
                new ChannelTop { Name = "Discovery History" },
                new ChannelTop { Name = "Discovery Science" },
                new ChannelTop { Name = "Sky History", AltNames = ["History (UK)"] },
            ],

            ["Entertainment"] =
            [
                new ChannelTop { Name = "Alibi" },
                new ChannelTop { Name = "BBC Alba" },
                new ChannelTop { Name = "BritBox" },
                new ChannelTop { Name = "Dave" },
                new ChannelTop { Name = "Drama" },
                new ChannelTop { Name = "Eden" },
                new ChannelTop { Name = "Gold" },
                new ChannelTop { Name = "Pick" },
                new ChannelTop { Name = "Quest" },
                new ChannelTop { Name = "Really" },
                new ChannelTop { Name = "Sky Atlantic" },
                new ChannelTop { Name = "Sky Max" },
                new ChannelTop { Name = "Sky Mix" },
                new ChannelTop { Name = "Sky Showcase" },
                new ChannelTop { Name = "Sky Witness" },
                new ChannelTop { Name = "W" },
                new ChannelTop { Name = "Yesterday" },
            ],

            ["Kids"] =
            [
                new ChannelTop { Name = "CBBC" },
                new ChannelTop { Name = "CBeebies" },
                new ChannelTop { Name = "Cartoonito" },
                new ChannelTop { Name = "POP" },
                new ChannelTop { Name = "Tiny Pop" },
            ],

            ["Main"] =
            [
                new ChannelTop { Name = "4seven" },
                new ChannelTop { Name = "5Action" },
                new ChannelTop { Name = "5Select" },
                new ChannelTop { Name = "5Star" },
                new ChannelTop { Name = "5USA" },
                new ChannelTop { Name = "BBC Four" },
                new ChannelTop { Name = "BBC One" },
                new ChannelTop { Name = "BBC Scotland" },
                new ChannelTop { Name = "BBC Three" },
                new ChannelTop { Name = "BBC Two" },
                new ChannelTop { Name = "Channel 4" },
                new ChannelTop { Name = "Channel 5" },
                new ChannelTop { Name = "E4" },
                new ChannelTop { Name = "ITV1" },
                new ChannelTop { Name = "ITV2" },
                new ChannelTop { Name = "ITV3" },
                new ChannelTop { Name = "ITV4" },
                new ChannelTop { Name = "ITVBe" },
                new ChannelTop { Name = "S4C" },
                new ChannelTop { Name = "STV" },
                new ChannelTop { Name = "UTV" },
            ],

            ["Movies"] =
            [
                new ChannelTop { Name = "Film4" },
                new ChannelTop { Name = "Sky Cinema Action" },
                new ChannelTop { Name = "Sky Cinema Animation" },
                new ChannelTop { Name = "Sky Cinema Comedy" },
                new ChannelTop { Name = "Sky Cinema Drama" },
                new ChannelTop { Name = "Sky Cinema Family" },
                new ChannelTop { Name = "Sky Cinema Greats" },
                new ChannelTop { Name = "Sky Cinema Premiere" },
                new ChannelTop
                {
                    Name = "Sky Cinema Sci-Fi & Horror",
                    AltNames = ["Sky Cinema Sci-Fi and Horror"],
                },
                new ChannelTop { Name = "Sky Cinema Thriller" },
            ],

            ["Music"] =
            [
                new ChannelTop { Name = "4Music" },
                new ChannelTop { Name = "Kerrang!" },
                new ChannelTop { Name = "Kiss TV" },
                new ChannelTop { Name = "The Box" },
            ],

            ["News"] =
            [
                new ChannelTop { Name = "Al Jazeera English", AltNames = ["Al Jazeera"] },
                new ChannelTop { Name = "BBC News" },
                new ChannelTop { Name = "BBC Parliament" },
                new ChannelTop { Name = "Euronews" },
                new ChannelTop { Name = "France 24 English", AltNames = ["France 24"] },
                new ChannelTop { Name = "GB News" },
                new ChannelTop { Name = "Sky News" },
                new ChannelTop { Name = "TalkTV" },
            ],

            ["Sports"] =
            [
                new ChannelTop { Name = "Eurosport 1" },
                new ChannelTop { Name = "Eurosport 2" },
                new ChannelTop { Name = "Premier Sports" },
                new ChannelTop { Name = "Sky Sports Cricket" },
                new ChannelTop { Name = "Sky Sports F1" },
                new ChannelTop { Name = "Sky Sports Football" },
                new ChannelTop { Name = "Sky Sports Golf" },
                new ChannelTop { Name = "Sky Sports Main Event" },
                new ChannelTop { Name = "Sky Sports News" },
                new ChannelTop { Name = "TNT Sports 1" },
                new ChannelTop { Name = "TNT Sports 2" },
                new ChannelTop { Name = "TNT Sports 3" },
                new ChannelTop { Name = "TNT Sports 4" },
            ],
        };
    }

    // Pure/static version for tests
    public static List<MenuEntry> Get247Entries(string rootTitle, IEnumerable<Channel> channels)
    {
        // ---- Local helpers ----
        static string CleanBaseName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var text = name;
            text = TagsRegex().Replace(text, ""); // remove [VIP], [HD], etc.
            text = TwoFourSevenRegex().Replace(text, ""); // remove 24/7
            text = SeasonShortRegex().Replace(text, ""); // strip "S01" etc. from base
            text = SeasonLongRegex().Replace(text, ""); // strip "Season 1" etc. from base
            text = NormalizeSpaceRegex().Replace(text, " ").Trim();
            return text;
        }

        static (string Base, string Season) ExtractBaseAndSeason(string originalName)
        {
            var baseName = CleanBaseName(originalName);

            // Prefer short season first (S01); if none, look for "Season 1"
            var seasonMatch = SeasonShortRegex().Match(originalName);
            if (!seasonMatch.Success)
            {
                seasonMatch = SeasonLongRegex().Match(originalName);
            }

            return (baseName, seasonMatch.Success ? seasonMatch.Value : null);
        }

        // ---- Select candidates: must START with "24/7" and not match the (AA) marker ----
        var candidates = channels
            .Where(ch =>
            {
                if (ch is null || string.IsNullOrWhiteSpace(ch.DisplayName))
                    return false;

                if (!StartsWith247Regex().IsMatch(ch.DisplayName))
                    return false;

                if (ch.DisplayName.IndexOf("Not 24/7", StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;

                if (MatchTwoParens().IsMatch(ch.DisplayName))
                    return false;

                return true;
            })
            .Select(ch => new { Channel = ch, Info = ExtractBaseAndSeason(ch.DisplayName) });

        // ---- Group by cleaned base name; sort groups & items deterministically ----
        var grouped = candidates
            .GroupBy(x => x.Info.Base, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                BaseName = g.Key,
                Items = g.OrderBy(
                        x => x.Info.Season ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ThenBy(x => x.Channel.DisplayName, StringComparer.OrdinalIgnoreCase),
            });

        // ---- Build entries ----
        var entries = new List<MenuEntry>();

        foreach (var group in grouped)
        {
            if (string.IsNullOrWhiteSpace(group.BaseName))
            {
                continue; // skip empties
            }

            char first = group.BaseName[0];
            string bucket;

            if (char.IsDigit(first))
            {
                bucket = "1-9";
            }
            else if (char.IsLetter(first))
            {
                bucket = char.ToUpperInvariant(first).ToString();
            }
            else
            {
                bucket = null;
            }

            if (bucket is null)
            {
                continue; // skip non-letter, non-digit groups
            }

            bool hasMultiple = group.Items.Skip(1).Any();

            foreach (var item in group.Items)
            {
                // Display text is the cleaned base plus season (if present)
                string display = group.BaseName;
                if (!string.IsNullOrEmpty(item.Info.Season))
                {
                    display = $"{display} {item.Info.Season}";
                }

                entries.Add(
                    new MenuEntry
                    {
                        Bucket = bucket,
                        GroupBase = hasMultiple ? group.BaseName : null, // null for singletons
                        DisplayText = display,
                        Channel = item.Channel,
                    }
                );
            }
        }

        // ---- Final ordering: Bucket -> (GroupBase or DisplayText) ----
        return
        [
            .. entries
                .OrderBy(e => e.Bucket, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.GroupBase ?? e.DisplayText, StringComparer.OrdinalIgnoreCase),
        ];
    }

    // ----------------- Regex helpers -----------------
    [GeneratedRegex(@"\([A-Za-z]{2}\)", RegexOptions.Compiled)]
    private static partial Regex MatchTwoParens();

    [GeneratedRegex(@"24\s*/\s*7", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TwoFourSevenRegex();

    [GeneratedRegex(@"\[[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TagsRegex();

    [GeneratedRegex(@"(?<!\w)S\d{2}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasonShortRegex();

    [GeneratedRegex(@"Season\s*\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SeasonLongRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex NormalizeSpaceRegex();

    [GeneratedRegex(@"^\s*24\s*/\s*7\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex StartsWith247Regex();
}