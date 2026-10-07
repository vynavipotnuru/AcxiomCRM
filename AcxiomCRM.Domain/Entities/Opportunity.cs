using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Entities;

public class Opportunity
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }
    public decimal Amount { get; set; }
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;
    public int Probability { get; set; } = 10; // 0 - 100
    public DateTime ExpectedCloseDate { get; set; }
    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;
    public string? AssignedTo { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Derived calculation: Amount * Probability / 100
    public decimal WeightedPipeline => Math.Round(Amount * (decimal)Probability / 100m, 2);

    // Navigation properties
    public virtual Customer? Customer { get; set; }
    public virtual Lead? Lead { get; set; }
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
