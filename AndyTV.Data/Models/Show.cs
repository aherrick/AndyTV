namespace AndyTV.Data.Models;

public class Show
{
    public string StreamingTVId { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public string Category { get; set; }
    public string Subject { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Description { get; set; }
}