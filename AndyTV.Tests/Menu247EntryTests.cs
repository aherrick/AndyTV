using AndyTV.Data.Models;
using AndyTV.Data.Services;
using Xunit;

namespace AndyTV.Tests;

// Shared data for this test class (constructed once per class)
public sealed class Menu247EntryFixture
{
    public List<MenuEntry> Entries { get; }

    public Menu247EntryFixture()
    {
        Entries = ChannelService.Get247Entries(
            "24/7",
            [
                new Channel { Name = "24/7 Seinfeld S01", Url = "s1" },
                new Channel { Name = "24/7 90210", Url = "u1" },
                new Channel { Name = "24/7 ALF", Url = "u2" },
                new Channel { Name = "24/7 The Simpsons Season 1", Url = "sim1" },
                new Channel { Name = "24/7 Action Movies", Url = "u3" },
                new Channel { Name = "24/7 21 JUMP STREET", Url = "u4" },
                new Channel { Name = "24/7 3 Ninjas Movies [VIP]", Url = "u5" },
                new Channel { Name = "24/7 300 Movies [VIP]", Url = "u6" },
                new Channel { Name = "24/7 Seinfeld S02", Url = "s2" },
                new Channel { Name = "24/7 The Simpsons Season 2", Url = "sim2" },
                new Channel { Name = "24/7 Friends (AL)", Url = "ex1" },
            ]
        );
    }
}

// C# 12 primary constructor + IClassFixture is fine
public class Menu247EntryTests(Menu247EntryFixture fx) : IClassFixture<Menu247EntryFixture>
{
    [Fact]
    public void ProducesBuckets_NumberAndLetters()
    {
        var buckets = fx
            .Entries.Select(e => e.Bucket)
            .Distinct() // preserves first-seen order
            .ToList();

        // Expect exactly four buckets, in this order: 1-9, A, S, T
        Assert.Collection(
            buckets,
            b => Assert.Equal("1-9", b),
            b => Assert.Equal("A", b),
            b => Assert.Equal("S", b),
            b => Assert.Equal("T", b)
        );

        // Excluded item should not be present at all
        Assert.DoesNotContain(fx.Entries, e => e.Channel.Url == "ex1");
    }

    [Fact]
    public void CleansDisplayText_RemovesTagsAnd247()
    {
        var ninjas = fx.Entries.FirstOrDefault(e => e.Channel.Url == "u5");
        Assert.NotNull(ninjas);
        Assert.Equal("3 Ninjas Movies", ninjas!.DisplayText); // no [VIP], no "24/7"

        var threeHundred = fx.Entries.FirstOrDefault(e => e.Channel.Url == "u6");
        Assert.NotNull(threeHundred);
        Assert.Equal("300 Movies", threeHundred!.DisplayText);
    }

    [Fact]
    public void GroupsByBaseName_AndOrdersBySeason()
    {
        var seinfeld = fx.Entries.Where(e => e.GroupBase == "Seinfeld").ToList();
        Assert.Collection(
            seinfeld.Select(e => e.DisplayText),
            s =>
            {
                Assert.Equal("Seinfeld S01", s);
            },
            s =>
            {
                Assert.Equal("Seinfeld S02", s);
            }
        );

        var simpsons = fx
            .Entries.Where(e =>
                string.Equals(
                    e.GroupBase,
                    "The Simpsons",
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            .ToList();

        Assert.Collection(
            simpsons.Select(e => e.DisplayText),
            s =>
            {
                Assert.Equal("The Simpsons Season 1", s);
            },
            s =>
            {
                Assert.Equal("The Simpsons Season 2", s);
            }
        );
    }

    [Fact]
    public void ExcludesTwoLetterParenCodes()
    {
        // Just verify the excluded URL is not present in the response
        Assert.DoesNotContain(fx.Entries, e => e.Channel.Url == "ex1");
    }

    [Fact]
    public void SingletonItems_HaveNullGroupBase_AndCorrectBucket()
    {
        var alf = fx.Entries.Single(e => e.Channel.Url == "u2");
        Assert.Null(alf.GroupBase);
        Assert.Equal("A", alf.Bucket);
        Assert.Equal("ALF", alf.DisplayText);

        var nineOh = fx.Entries.Single(e => e.Channel.Url == "u1");
        Assert.Null(nineOh.GroupBase);
        Assert.Equal("1-9", nineOh.Bucket);
        Assert.Equal("90210", nineOh.DisplayText);
    }
}