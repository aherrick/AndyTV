using System.Threading.Channels;

namespace AndyTV.Tests
{
    public class UnitTest1
    {
        private static List<Channel> SampleChannels() =>
            [
                new Channel { DisplayName = "24/7 90210", Url = "u1" },
                new Channel { DisplayName = "24/7 ALF", Url = "u2" },
                new Channel { DisplayName = "24/7 Action Movies", Url = "u3" },
                new Channel { DisplayName = "24/7 21 JUMP STREET", Url = "u4" },
                new Channel { DisplayName = "24/7 3 Ninjas Movies [VIP]", Url = "u5" },
                new Channel { DisplayName = "24/7 300 Movies [VIP]", Url = "u6" },
                new Channel { DisplayName = "24/7 (DE) Test Channel", Url = "u7" }, // filtered
                new Channel { DisplayName = "24/7 Batman", Url = "u8" },
                new Channel { DisplayName = "24/7 Batman S3", Url = "u9" },
                new Channel { DisplayName = "24/7 Batman S4", Url = "u10" },
                new Channel { DisplayName = "24/7 Abbott Elementary", Url = "u11" },
                new Channel { DisplayName = "24/7 A Nightmare On Elm Street", Url = "u12" },
                new Channel
                {
                    DisplayName = "24/7 A Nightmare On Elm Street Movies [VIP]",
                    Url = "u13",
                },
                new Channel { DisplayName = "24/7 Seinfeld Season 1", Url = "s1" },
                new Channel { DisplayName = "24/7 Seinfeld Season 2", Url = "s2" },
                new Channel { DisplayName = "24/7 The Simpsons S1", Url = "x1" },
                new Channel { DisplayName = "24/7 The Simpsons S2", Url = "x2" },
            ];

        [Fact]
        public void Test1()
        { }
    }
}