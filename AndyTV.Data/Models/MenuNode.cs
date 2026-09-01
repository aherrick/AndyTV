namespace AndyTV.Data.Models;

public sealed class MenuNode
{
    public string Text { get; set; }

    // Non-null marks a leaf that plays this channel; otherwise Children form a submenu.
    public Channel Channel { get; set; }

    public List<MenuNode> Children { get; set; }
}
