namespace AndyTV.Data.Models;

public class Playlist
{
    public string Name { get; set; }
    public string Url { get; set; }
    public bool ShowInMenu { get; set; }
    public bool GroupByFirstChar { get; set; }
    public string UrlFind { get; set; }
    public string UrlReplace { get; set; }
}
