using System.Text.Json.Serialization;

namespace AndyTV.Data.Models;

public class Playlist
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool ShowInMenu { get; set; }
    public bool GroupByFirstChar { get; set; }
    public string UrlFind { get; set; }
    public string UrlReplace { get; set; }
    public string NameFind { get; set; }
    public string NameReplace { get; set; }

    // M3U group-titles to surface as category submenus. Empty/null = show all.
    public List<string> Groups { get; set; }

    [JsonIgnore]
    public string GroupsText
    {
        get => Groups is null ? string.Empty : string.Join("; ", Groups);
        set =>
            Groups =
            [
                .. (value ?? string.Empty).Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                ),
            ];
    }
}