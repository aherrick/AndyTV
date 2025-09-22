using AndyTV.Guide.Models;

namespace AndyTV.Guide.Services;

public static class TVService
{
    // Category (section) IDs
    private const int NewsId = 1;

    private const int SportsId = 2;
    private const int EntId = 3;

    // Channel IDs
    private const int NbcId = 10;

    private const int CbsId = 11;
    private const int AbcId = 12;

    private const int EspnId = 20;
    private const int Espn2Id = 21;
    private const int Fs1Id = 22;

    private const int UsaId = 30;
    private const int HistId = 31;

    // Base date (local)
    private static readonly DateTime GuideDate = new(2025, 9, 22, 0, 0, 0, DateTimeKind.Local);

    // Project window: 8:00 AM through midnight (next day 00:00)
    public static readonly DateTime ProjectStart = new(2025, 9, 22, 8, 0, 0, DateTimeKind.Local);

    public static readonly DateTime ProjectEnd = new(2025, 9, 23, 0, 0, 0, DateTimeKind.Local);

    // Deterministic "random" so the UI is stable between runs
    private static readonly Random Rng = new(19790912);

    private static DateTime At(int hour, int minute)
    {
        return new DateTime(
            GuideDate.Year,
            GuideDate.Month,
            GuideDate.Day,
            hour,
            minute,
            0,
            DateTimeKind.Local
        );
    }

    public static List<GuideResource> GetResources()
    {
        // Categories (parents)
        var resources = new List<GuideResource>
        {
            new()
            {
                Id = NewsId,
                Name = "News",
                IsSection = true,
                ParentId = null,
            },
            new()
            {
                Id = SportsId,
                Name = "Sports",
                IsSection = true,
                ParentId = null,
            },
            new()
            {
                Id = EntId,
                Name = "Entertainment",
                IsSection = true,
                ParentId = null,
            },
            // News channels
            new()
            {
                Id = NbcId,
                Name = "NBC",
                IsSection = false,
                ParentId = NewsId,
            },
            new()
            {
                Id = CbsId,
                Name = "CBS",
                IsSection = false,
                ParentId = NewsId,
            },
            new()
            {
                Id = AbcId,
                Name = "ABC",
                IsSection = false,
                ParentId = NewsId,
            },
            // Sports channels
            new()
            {
                Id = EspnId,
                Name = "ESPN",
                IsSection = false,
                ParentId = SportsId,
            },
            new()
            {
                Id = Espn2Id,
                Name = "ESPN2",
                IsSection = false,
                ParentId = SportsId,
            },
            new()
            {
                Id = Fs1Id,
                Name = "FS1",
                IsSection = false,
                ParentId = SportsId,
            },
            // Entertainment channels
            new()
            {
                Id = UsaId,
                Name = "USA Network",
                IsSection = false,
                ParentId = EntId,
            },
            new()
            {
                Id = HistId,
                Name = "History",
                IsSection = false,
                ParentId = EntId,
            },
        };

        return resources;
    }

    public static List<GuideTask> GetTasks()
    {
        // Title pools (some repeats, some unique per category)
        string[] newsTitles =
        [
            "Morning Update",
            "Daytime Briefing",
            "Top Stories",
            "Local Live",
            "World Watch",
            "Economy Now",
            "Weather First",
            "Investigates",
            "Evening Desk",
            "Nightline",
        ];

        string[] sportsTitles =
        [
            "SportsCenter",
            "Kickoff",
            "NFL Live",
            "Diamond Talk",
            "College Hub",
            "Matchday",
            "Tape Room",
            "Highlights",
            "Postgame",
            "Overtime",
        ];

        string[] entTitles =
        [
            "Comedy Hour",
            "Drama Replay",
            "Game Show",
            "Docu Spotlight",
            "Retro TV",
            "Crime Files",
            "Reality Check",
            "Feature Film",
            "Mini-Series",
            "Late Night",
        ];

        // Channels by category
        var newsChannels = new[] { NbcId, CbsId, AbcId };
        var sportsChannels = new[] { EspnId, Espn2Id, Fs1Id };
        var entChannels = new[] { UsaId, HistId };

        int taskId = 1;
        var tasks = new List<GuideTask>();

        // Generate a full schedule 8:00 → 24:00 for every channel
        foreach (var ch in newsChannels)
        {
            GenerateChannelSchedule(ch, newsTitles, ref taskId, tasks);
        }
        foreach (var ch in sportsChannels)
        {
            GenerateChannelSchedule(ch, sportsTitles, ref taskId, tasks);
        }
        foreach (var ch in entChannels)
        {
            GenerateChannelSchedule(ch, entTitles, ref taskId, tasks);
        }

        return tasks;
    }

    public static List<GuideAssignment> GetAssignments()
    {
        // Link every task to its channel (leaf resource)
        var tasks = GetTasks();
        int pk = 1;
        var assignments = new List<GuideAssignment>(tasks.Count);

        foreach (var t in tasks)
        {
            // We tuck the resource id into Description during generation; parse it out here.
            // If you prefer, add ResourceId directly to GuideTask in your model instead.
            if (TryReadChannelId(t.Description, out var resourceId))
            {
                assignments.Add(
                    new GuideAssignment
                    {
                        PrimaryId = pk++,
                        TaskId = t.Id,
                        ResourceId = resourceId,
                    }
                );

                // Clean Description (remove the marker)
                var ix = t.Description.IndexOf("::RID=", StringComparison.Ordinal);
                if (ix >= 0)
                {
                    t.Description = t.Description[..ix].TrimEnd();
                }
            }
        }

        return assignments;
    }

    // ----------------- helpers -----------------

    private static void GenerateChannelSchedule(
        int channelId,
        string[] titlePool,
        ref int nextTaskId,
        List<GuideTask> tasks
    )
    {
        var cursor = ProjectStart;
        while (cursor < ProjectEnd)
        {
            // Pick 30 or 60 minutes, but don't spill past midnight
            int minutes = Rng.Next(0, 2) == 0 ? 30 : 60;
            var next = cursor.AddMinutes(minutes);
            if (next > ProjectEnd)
            {
                next = ProjectEnd;
            }

            // Choose a title (allow repeats)
            var title = titlePool[Rng.Next(titlePool.Length)];

            tasks.Add(
                new GuideTask
                {
                    Id = nextTaskId++,
                    Name = title,
                    Description = $"{title} on channel {channelId} ::RID={channelId}", // marker for assignment
                    Start = cursor,
                    End = next,
                }
            );

            cursor = next;
        }
    }

    private static bool TryReadChannelId(string description, out int channelId)
    {
        channelId = 0;
        if (string.IsNullOrEmpty(description))
        {
            return false;
        }

        const string tag = "::RID=";
        var ix = description.IndexOf(tag, StringComparison.Ordinal);
        if (ix < 0)
        {
            return false;
        }

        var start = ix + tag.Length;
        var digits = new System.Text.StringBuilder();
        for (int i = start; i < description.Length; i++)
        {
            char c = description[i];
            if (char.IsDigit(c))
            {
                digits.Append(c);
            }
            else
            {
                break;
            }
        }

        if (digits.Length == 0)
        {
            return false;
        }

        if (int.TryParse(digits.ToString(), out var id))
        {
            channelId = id;
            return true;
        }

        return false;
    }
}