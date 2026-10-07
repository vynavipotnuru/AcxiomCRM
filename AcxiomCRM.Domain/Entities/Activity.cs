using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class Activity
{
    public int ActivityId { get; set; }
    public ActivityType ActivityType { get; set; } = ActivityType.Call;
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public string? AssignedTo { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Customer? Customer { get; set; }
    public virtual Lead? Lead { get; set; }
}
