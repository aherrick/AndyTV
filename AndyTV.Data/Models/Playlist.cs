namespace AndyTV.Data.Models;

public class Playlist
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool ShowInMenu { get; set; }
    // Whether this playlist's channels feed the curated US/UK lists (default in so
    // existing playlists are unaffected; untick for TV-show/movie playlists).
    public bool ShowInUsUk { get; set; } = true;
    public bool GroupByFirstChar { get; set; }
    public string NameFind { get; set; }
    public string NameReplace { get; set; }
}