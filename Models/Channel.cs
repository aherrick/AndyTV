namespace AndyTV.Models;

public class Channel
{
    public string Name { get; set; }
    public string Group { get; set; }
    public string Url { get; set; }

    public string MappedName { get; set; }
    public string MappedGroup { get; set; }

    public string DisplayName => MappedName ?? Name;

    public string DisplayGroup => MappedGroup ?? Group;
}