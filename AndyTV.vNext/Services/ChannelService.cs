namespace AndyTV.vNext;

static class ChannelService
{
    // Curated "top" channels, always shown in the menu. Matching against loaded
    // playlists is case-insensitive on Name and any AltNames.
    public static Dictionary<string, List<ChannelTop>> TopUs { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["24/7"] =
        [
            new() { Name = "Breaking Bad" },
            new() { Name = "Forensic Files" },
            new() { Name = "Frasier" },
            new() { Name = "Friends" },
            new() { Name = "Game of Thrones" },
            new() { Name = "I Love Lucy" },
            new() { Name = "Saturday Night Live", AltNames = ["SNL"] },
            new() { Name = "Seinfeld" },
            new() { Name = "The Office" },
            new() { Name = "The Simpsons" },
            new() { Name = "The Sopranos" },
            new() { Name = "The Wire" },
            new() { Name = "Unsolved Mysteries" },
        ],
        ["Entertainment"] =
        [
            new() { Name = "A&E", AltNames = ["AE"] },
            new() { Name = "AMC" },
            new() { Name = "Animal Planet" },
            new() { Name = "Bravo" },
            new() { Name = "Comedy Central" },
            new() { Name = "Cooking Channel" },
            new() { Name = "Crime + Investigation", AltNames = ["Crime & Investigation", "Crime and Investigation"] },
            new() { Name = "Discovery Channel", AltNames = ["Discovery"] },
            new() { Name = "Discovery Family" },
            new() { Name = "Disney Channel", AltNames = ["Disney"] },
            new() { Name = "E!", AltNames = ["E! Entertainment"] },
            new() { Name = "Food Network", AltNames = ["Food"] },
            new() { Name = "Freeform" },
            new() { Name = "FX" },
            new() { Name = "FXX" },
            new() { Name = "FX Movie Channel", AltNames = ["FXM"] },
            new() { Name = "FYI" },
            new() { Name = "Hallmark Channel", AltNames = ["Hallmark"] },
            new() { Name = "Hallmark Drama" },
            new() { Name = "Hallmark Movies & Mysteries", AltNames = ["Hallmark Movies and Mysteries", "HMM"] },
            new() { Name = "HGTV" },
            new() { Name = "History", AltNames = ["History Channel"] },
            new() { Name = "IFC" },
            new() { Name = "Investigation Discovery", AltNames = ["ID"] },
            new() { Name = "Lifetime" },
            new() { Name = "Magnolia Network" },
            new() { Name = "MotorTrend", AltNames = ["Motor Trend"] },
            new() { Name = "National Geographic", AltNames = ["Nat Geo"] },
            new() { Name = "National Geographic Wild", AltNames = ["Nat Geo Wild"] },
            new() { Name = "OWN", AltNames = ["Oprah Winfrey Network"] },
            new() { Name = "Oxygen" },
            new() { Name = "Paramount Network", AltNames = ["Paramount"] },
            new() { Name = "Pop TV", AltNames = ["Pop"] },
            new() { Name = "Science Channel", AltNames = ["Science"] },
            new() { Name = "Smithsonian Channel", AltNames = ["Smithsonian"] },
            new() { Name = "Sundance TV", AltNames = ["SundanceTV"] },
            new() { Name = "Syfy", AltNames = ["Sci-Fi"] },
            new() { Name = "TBS" },
            new() { Name = "TCM", AltNames = ["Turner Classic Movies"] },
            new() { Name = "TLC" },
            new() { Name = "TNT" },
            new() { Name = "Travel Channel" },
            new() { Name = "TruTV" },
            new() { Name = "USA Network", AltNames = ["USA"] },
            new() { Name = "VH1" },
            new() { Name = "WE TV" },
        ],
        ["Kids"] =
        [
            new() { Name = "Boomerang" },
            new() { Name = "Cartoon Network", AltNames = ["CN"] },
            new() { Name = "Disney Junior" },
            new() { Name = "Disney XD" },
            new() { Name = "Nick Jr", AltNames = ["Nick Junior"] },
            new() { Name = "Nickelodeon", AltNames = ["Nick"] },
            new() { Name = "Nicktoons" },
            new() { Name = "PBS Kids" },
            new() { Name = "TeenNick" },
            new() { Name = "Universal Kids" },
        ],
        ["Locals"] =
        [
            new() { Name = "ABC" },
            new() { Name = "Antenna TV", AltNames = ["Antenna"] },
            new() { Name = "Bounce" },
            new() { Name = "CBS" },
            new() { Name = "Charge!" },
            new() { Name = "Comet" },
            new() { Name = "Cozi TV", AltNames = ["Cozi"] },
            new() { Name = "CW" },
            new() { Name = "FOX" },
            new() { Name = "Grit" },
            new() { Name = "Ion Mystery", AltNames = ["ION Mystery", "Ion Plus Mystery"] },
            new() { Name = "Laff" },
            new() { Name = "MeTV" },
            new() { Name = "MyNetworkTV", AltNames = ["MyNetwork TV", "MyTV"] },
            new() { Name = "NBC" },
            new() { Name = "PBS" },
            new() { Name = "Start TV", AltNames = ["StartTV"] },
        ],
        ["Movies"] =
        [
            new() { Name = "5StarMax" },
            new() { Name = "ActionMax" },
            new() { Name = "Cinemax" },
            new() { Name = "Epix", AltNames = ["MGM+"] },
            new() { Name = "Epix 2", AltNames = ["MGM+ 2"] },
            new() { Name = "Epix Drive-In", AltNames = ["MGM+ Drive-In", "MGM+ Drive In"] },
            new() { Name = "Epix Hits", AltNames = ["MGM+ Hits"] },
            new() { Name = "Flix" },
            new() { Name = "HBO" },
            new() { Name = "HBO 2" },
            new() { Name = "HBO Comedy" },
            new() { Name = "HBO Family" },
            new() { Name = "HBO Latino" },
            new() { Name = "HBO Signature" },
            new() { Name = "HBO Zone" },
            new() { Name = "MoreMax" },
            new() { Name = "Showtime" },
            new() { Name = "Showtime 2" },
            new() { Name = "Showtime Extreme" },
            new() { Name = "Showtime Family Zone" },
            new() { Name = "Showtime Next" },
            new() { Name = "Showtime Showcase" },
            new() { Name = "Showtime Women" },
            new() { Name = "Sony Movie Channel" },
            new() { Name = "Starz" },
            new() { Name = "Starz Cinema" },
            new() { Name = "Starz Comedy" },
            new() { Name = "Starz Edge" },
            new() { Name = "Starz Encore" },
            new() { Name = "Starz Encore Action" },
            new() { Name = "Starz Encore Black" },
            new() { Name = "Starz Encore Classic" },
            new() { Name = "Starz Encore Family" },
            new() { Name = "Starz Encore Suspense" },
            new() { Name = "Starz Encore Westerns" },
            new() { Name = "Starz Kids & Family", AltNames = ["Starz Kids and Family"] },
            new() { Name = "The Movie Channel", AltNames = ["TMC"] },
            new() { Name = "The Movie Channel Xtra", AltNames = ["TMC Xtra", "TMCXtra"] },
        ],
        ["Music"] =
        [
            new() { Name = "AXS TV", AltNames = ["AXS"] },
            new() { Name = "BET" },
            new() { Name = "BET Her" },
            new() { Name = "BET Jams" },
            new() { Name = "BET Soul" },
            new() { Name = "CMT" },
            new() { Name = "CMT Music" },
            new() { Name = "Fuse" },
            new() { Name = "MTV" },
            new() { Name = "MTV Classic" },
            new() { Name = "MTV2" },
            new() { Name = "MTVU" },
            new() { Name = "Music Choice" },
            new() { Name = "Revolt" },
        ],
        ["News"] =
        [
            new() { Name = "ABC News" },
            new() { Name = "Bloomberg" },
            new() { Name = "CBS News" },
            new() { Name = "CNBC" },
            new() { Name = "CNN" },
            new() { Name = "CSPAN", AltNames = ["C-SPAN"] },
            new() { Name = "CSPAN 2", AltNames = ["C-SPAN 2"] },
            new() { Name = "Fox Business", AltNames = ["Fox Business Network"] },
            new() { Name = "Fox News", AltNames = ["Fox News Channel"] },
            new() { Name = "HLN", AltNames = ["Headline News"] },
            new() { Name = "MSNBC" },
            new() { Name = "NBC News" },
            new() { Name = "NewsNation" },
            new() { Name = "Newsmax" },
            new() { Name = "OANN", AltNames = ["One America News", "One America News Network"] },
            new() { Name = "The Weather Channel", AltNames = ["Weather Channel"] },
        ],
        ["Other"] =
        [
            new() { Name = "BBC America" },
            new() { Name = "Court TV", AltNames = ["CourtTV"] },
            new() { Name = "GSN", AltNames = ["Game Show Network"] },
            new() { Name = "ION", AltNames = ["ION Television"] },
            new() { Name = "ION Plus", AltNames = ["IonPlus"] },
            new() { Name = "Reelz" },
            new() { Name = "Trinity Broadcasting", AltNames = ["TBN", "Trinity Broadcasting Network"] },
        ],
        ["Sports"] =
        [
            new() { Name = "ACC Network", AltNames = ["ACCN"] },
            new() { Name = "Bally Sports" },
            new() { Name = "beIN Sports", AltNames = ["beIN Sports USA"] },
            new() { Name = "Big Ten Network", AltNames = ["BTN"] },
            new() { Name = "CBS Sports Network", AltNames = ["CBSSN"] },
            new() { Name = "ESPN" },
            new() { Name = "ESPN+", AltNames = ["ESPN Plus", "ESPNPLUS"] },
            new() { Name = "ESPN 2", AltNames = ["ESPN2"] },
            new() { Name = "ESPN News", AltNames = ["ESPNews"] },
            new() { Name = "ESPNU" },
            new() { Name = "Fox Sports 1", AltNames = ["FS1"] },
            new() { Name = "Fox Sports 2", AltNames = ["FS2"] },
            new() { Name = "Golf Channel" },
            new() { Name = "Marquee Sports Network", AltNames = ["Marquee"] },
            new() { Name = "MLB Network" },
            new() { Name = "MSG", AltNames = ["Madison Square Garden"] },
            new() { Name = "NBA TV", AltNames = ["NBATV"] },
            new() { Name = "NESN", AltNames = ["New England Sports Network"] },
            new() { Name = "NFL Network" },
            new() { Name = "NFL RedZone", AltNames = ["RedZone"] },
            new() { Name = "NHL Network" },
            new() { Name = "SEC Network", AltNames = ["SECN"] },
            new() { Name = "Tennis Channel" },
            new() { Name = "YES Network" },
        ],
    };

    public static Dictionary<string, List<ChannelTop>> TopUk { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Documentary"] =
        [
            new() { Name = "Crime+Investigation", AltNames = ["Crime & Investigation", "Crime and Investigation"] },
            new() { Name = "Discovery History" },
            new() { Name = "Discovery Science" },
            new() { Name = "PBS America" },
            new() { Name = "Sky History", AltNames = ["History (UK)"] },
        ],
        ["Entertainment"] =
        [
            new() { Name = "Alibi", AltNames = ["U&Alibi"] },
            new() { Name = "BBC Alba" },
            new() { Name = "Blaze" },
            new() { Name = "BritBox" },
            new() { Name = "Challenge" },
            new() { Name = "Dave", AltNames = ["U&Dave"] },
            new() { Name = "Drama", AltNames = ["U&Drama"] },
            new() { Name = "Eden", AltNames = ["U&Eden"] },
            new() { Name = "Gold", AltNames = ["U&Gold"] },
            new() { Name = "GREAT! TV" },
            new() { Name = "Legend" },
            new() { Name = "Pick" },
            new() { Name = "Quest" },
            new() { Name = "Really", AltNames = ["U&Really"] },
            new() { Name = "Sky Atlantic" },
            new() { Name = "Sky Max" },
            new() { Name = "Sky Mix" },
            new() { Name = "Sky Showcase" },
            new() { Name = "Sky Witness" },
            new() { Name = "That's TV" },
            new() { Name = "W", AltNames = ["U&W"] },
            new() { Name = "Yesterday", AltNames = ["U&Yesterday"] },
        ],
        ["Kids"] =
        [
            new() { Name = "CBBC" },
            new() { Name = "CBeebies" },
            new() { Name = "Cartoonito" },
            new() { Name = "POP" },
            new() { Name = "Tiny Pop" },
        ],
        ["Main"] =
        [
            new() { Name = "4seven" },
            new() { Name = "5Action" },
            new() { Name = "5Select" },
            new() { Name = "5Star" },
            new() { Name = "5USA" },
            new() { Name = "BBC Four" },
            new() { Name = "BBC One" },
            new() { Name = "BBC Scotland" },
            new() { Name = "BBC Three" },
            new() { Name = "BBC Two" },
            new() { Name = "Channel 4" },
            new() { Name = "Channel 5" },
            new() { Name = "E4" },
            new() { Name = "ITV1" },
            new() { Name = "ITV2" },
            new() { Name = "ITV3" },
            new() { Name = "ITV4" },
            new() { Name = "ITVBe" },
            new() { Name = "S4C" },
            new() { Name = "STV" },
            new() { Name = "UTV" },
        ],
        ["Movies"] =
        [
            new() { Name = "Film4" },
            new() { Name = "GREAT! Movies" },
            new() { Name = "Sky Cinema Action" },
            new() { Name = "Sky Cinema Animation" },
            new() { Name = "Sky Cinema Comedy" },
            new() { Name = "Sky Cinema Drama" },
            new() { Name = "Sky Cinema Family" },
            new() { Name = "Sky Cinema Greats" },
            new() { Name = "Sky Cinema Premiere" },
            new() { Name = "Sky Cinema Sci-Fi & Horror", AltNames = ["Sky Cinema Sci-Fi and Horror"] },
            new() { Name = "Sky Cinema Thriller" },
            new() { Name = "Talking Pictures TV" },
        ],
        ["Music"] =
        [
            new() { Name = "4Music" },
            new() { Name = "Kerrang!" },
            new() { Name = "Kiss TV" },
            new() { Name = "The Box" },
        ],
        ["News"] =
        [
            new() { Name = "Al Jazeera English", AltNames = ["Al Jazeera"] },
            new() { Name = "BBC News" },
            new() { Name = "BBC Parliament" },
            new() { Name = "CNN International" },
            new() { Name = "Euronews" },
            new() { Name = "France 24 English", AltNames = ["France 24"] },
            new() { Name = "GB News" },
            new() { Name = "Sky News" },
            new() { Name = "TalkTV" },
        ],
        ["Sports"] =
        [
            new() { Name = "Eurosport 1" },
            new() { Name = "Eurosport 2" },
            new() { Name = "Premier Sports" },
            new() { Name = "Sky Sports Arena" },
            new() { Name = "Sky Sports Cricket" },
            new() { Name = "Sky Sports F1" },
            new() { Name = "Sky Sports Football" },
            new() { Name = "Sky Sports Golf" },
            new() { Name = "Sky Sports Main Event" },
            new() { Name = "Sky Sports Mix" },
            new() { Name = "Sky Sports News" },
            new() { Name = "Sky Sports Premier League" },
            new() { Name = "Sky Sports Racing" },
            new() { Name = "TNT Sports 1" },
            new() { Name = "TNT Sports 2" },
            new() { Name = "TNT Sports 3" },
            new() { Name = "TNT Sports 4" },
        ],
    };

    // Name -> every matching channel across all loaded playlists.
    public static Dictionary<string, List<ChannelRef>> BuildLookup(
        IEnumerable<(PlaylistRef Ref, List<ChannelRef> Channels)> loaded)
    {
        var map = new Dictionary<string, List<ChannelRef>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, channels) in loaded)
            foreach (var c in channels)
            {
                if (!map.TryGetValue(c.Name, out var list))
                    map[c.Name] = list = [];
                if (!list.Any(x => string.Equals(x.Url, c.Url, StringComparison.OrdinalIgnoreCase)))
                    list.Add(c);
            }
        return map;
    }

    public static List<ChannelRef> Match(ChannelTop top, Dictionary<string, List<ChannelRef>> lookup)
    {
        var result = new List<ChannelRef>();
        if (lookup.TryGetValue(top.Name, out var hit))
            result.AddRange(hit);
        foreach (var alt in top.AltNames)
            if (lookup.TryGetValue(alt, out var altHit))
                result.AddRange(altHit);
        return result;
    }

    public static IEnumerable<IGrouping<string, ChannelRef>> GroupByFirst(IEnumerable<ChannelRef> channels) =>
        channels
            .GroupBy(c => FirstKey(c.Name), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

    private static string FirstKey(string name)
    {
        var ch = name.TrimStart().FirstOrDefault();
        return char.IsLetter(ch) ? char.ToUpperInvariant(ch).ToString() : "#";
    }
}
