namespace AndyTV.Data.Models;

public class Channel
{
    public string Name { get; set; } = string.Empty;
    public string MappedName { get; set; }

    public string Url { get; set; } = string.Empty;

    public string Group { get; set; }
    public string Category { get; set; }

    public string DisplayName => MappedName ?? Name;
}