namespace AndyTV.Models;

public class Channel
{
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public string MappedName { get; set; }
    public string MappedGroup { get; set; }

    // Always prefer mapped values, fallback to original
    public string DisplayName => !string.IsNullOrWhiteSpace(MappedName) ? MappedName : Name;

    public string DisplayGroup => !string.IsNullOrWhiteSpace(MappedGroup) ? MappedGroup : Group;
}