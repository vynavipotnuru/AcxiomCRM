using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.BusinessRules;

public static class LeadStatusTransitionRule
{
    private static readonly Dictionary<LeadStatus, List<LeadStatus>> AllowedTransitions = new()
    {
        [LeadStatus.New] = new() { LeadStatus.Contacted, LeadStatus.Qualified, LeadStatus.Unqualified, LeadStatus.Lost },
        [LeadStatus.Contacted] = new() { LeadStatus.Qualified, LeadStatus.Unqualified, LeadStatus.Lost },
        [LeadStatus.Qualified] = new() { LeadStatus.Converted, LeadStatus.Lost, LeadStatus.Unqualified },
        [LeadStatus.Unqualified] = new() { LeadStatus.Contacted, LeadStatus.Lost },
        [LeadStatus.Converted] = new(), // Terminal state
        [LeadStatus.Lost] = new() { LeadStatus.Contacted } // Can be re-opened for contact
    };

    public static bool IsValidTransition(LeadStatus currentStatus, LeadStatus targetStatus)
    {
        if (currentStatus == targetStatus) return true;
        if (AllowedTransitions.TryGetValue(currentStatus, out var allowed))
        {
            return allowed.Contains(targetStatus);
        }
        return false;
    }

    public static void ValidateTransition(LeadStatus currentStatus, LeadStatus targetStatus)
    {
        if (!IsValidTransition(currentStatus, targetStatus))
        {
            throw new BusinessRuleException($"Invalid lead status transition from '{currentStatus}' to '{targetStatus}'.");
        }
    }
}
