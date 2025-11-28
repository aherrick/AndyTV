namespace AndyTV.Data.Models;

public class Playlist
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool ShowInMenu { get; set; }
    public bool GroupByFirstChar { get; set; }
    public string UrlFind { get; set; }
    public string UrlReplace { get; set; }
}
