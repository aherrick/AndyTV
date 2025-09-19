namespace AndyTV.Data.Models;

public class Channel
{
    public string Name { get; set; }
    public string MappedName { get; set; }

    public string Url { get; set; }

    public string Group { get; set; }
    public string Category { get; set; }

    public string DisplayName => MappedName ?? Name;
}