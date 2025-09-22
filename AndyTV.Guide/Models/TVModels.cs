namespace AndyTV.Guide.Models;

public class GuideTask
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Description { get; set; }
}

public class GuideResource
{
    public int Id { get; set; }
    public int? ParentId { get; set; }

    public string Name { get; set; } // e.g., "1 NEWS" or "10 NBC"
    public bool IsSection { get; set; } // true => header row; no assignments
}

public class GuideAssignment
{
    public int PrimaryId { get; set; }
    public int TaskId { get; set; }
    public int ResourceId { get; set; }
    public int Unit { get; set; } = 100; // keep default
}