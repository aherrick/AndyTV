using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AndyTV.Models;
using AndyTV.Services;

namespace AndyTV.Helpers.Menu
{
    public class MenuTVChannelHelper(ContextMenuStrip menu)
    {
        private sealed class LazyPayload(string[] terms)
        {
            public string[] Terms { get; } = terms;
            public bool Loaded { get; set; }
        }

        public List<Channel> Channels { get; private set; } = [];

        // ---------- Load ----------

        public async Task LoadChannels(string m3uURL)
        {
            var parsed = await Task.Run(() => M3UService.ParseM3U(m3uURL));
            Channels = [.. parsed.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];
        }

        public void BuildMenu(EventHandler channelClick)
        {
            var us = BuildTopUs();
            var uk = BuildTopUk();

            menu.BeginInvoke(() =>
            {
                MenuHelper.AddHeader(menu, "TOP CHANNELS");

                var usItem = BuildTopMenuLazy("US", us, channelClick);
                menu.Items.Add(usItem);

                var ukItem = BuildTopMenuLazy("UK", uk, channelClick);
                menu.Items.Add(ukItem);
            });
        }

        private ToolStripMenuItem BuildTopMenuLazy(
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
                    var candidates = entry;

                    var parent = CreateLazyParent(display, candidates, channelClick);
                    catItem.DropDownItems.Add(parent);
                }

                if (catItem.DropDownItems.Count > 0)
                {
                    rootItem.DropDownItems.Add(catItem);
                }
            }

            return rootItem.DropDownItems.Count > 0 ? rootItem : null;
        }

        private ToolStripMenuItem CreateLazyParent(
            string display,
            string[] candidates,
            EventHandler channelClick
        )
        {
            var payload = new LazyPayload(candidates);
            var parent = new ToolStripMenuItem(display) { Tag = payload };

            // Force arrow so hover can open immediately, even before items exist
            parent.DropDown = new ToolStripDropDownMenu();

            // Hover to open (no click)
            parent.MouseEnter += (_, __) =>
            {
                if (!parent.DropDown.Visible)
                {
                    parent.ShowDropDown();
                }
            };

            // Populate once, on open
            parent.DropDownOpening += async (s, e) =>
            {
                var p = (ToolStripMenuItem)s;
                var data = (LazyPayload)p.Tag;

                if (data.Loaded)
                {
                    return;
                }

                var matches = await Task.Run(
                    () =>
                        Channels
                            .Where(ch =>
                                data.Terms.Any(term =>
                                    ch.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                                )
                            )
                            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                            .ToList()
                );

                if (matches.Count > 0)
                {
                    var items = new ToolStripItem[matches.Count];
                    for (int i = 0; i < matches.Count; i++)
                    {
                        var ch = matches[i];
                        var item = new ToolStripMenuItem(ch.Name) { Tag = ch };
                        item.Click += channelClick;
                        items[i] = item;
                    }
                    p.DropDownItems.AddRange(items);
                }

                data.Loaded = true; // even if empty, don't re-query on every hover
            };

            return parent;
        }

        // ---------- Utility ----------

        public Channel ChannelByUrl(string url)
        {
            return Channels.FirstOrDefault(ch =>
                string.Equals(ch.Url.Trim(), url.Trim(), StringComparison.OrdinalIgnoreCase)
            );
        }

        // ---------- Catalogs ----------

        private static Dictionary<string, string[][]> BuildTopUs() =>
            new(StringComparer.OrdinalIgnoreCase)
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
                    ["HLN"],
                    ["MSNBC"],
                    ["NBC News"],
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

        private static Dictionary<string, string[][]> BuildTopUk() =>
            new(StringComparer.OrdinalIgnoreCase)
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
}