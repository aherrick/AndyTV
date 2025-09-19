namespace AndyTV.Data.Models;

public class MenuEntry
{
    public string Bucket { get; set; } // "A", "B", ..., "1-9"
    public string GroupBase { get; set; } // null for singletons; base for grouped shows
    public string DisplayText { get; set; } // cleaned text for menu
    public Channel Channel { get; set; } // original channel
}