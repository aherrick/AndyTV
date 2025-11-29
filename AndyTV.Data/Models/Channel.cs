namespace AndyTV.Data.Models;

public class Channel
{
    // Raw title as it appeared in the source playlist (e.g. with HD / episode info)
    public string RawName { get; set; }

    public string Name { get; set; } = string.Empty;
    public string MappedName { get; set; }

    public string Url { get; set; } = string.Empty;

    public string Group { get; set; }
    public string Category { get; set; }

    public string DisplayName => MappedName ?? Name;
}