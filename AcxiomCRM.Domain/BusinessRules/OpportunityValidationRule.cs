using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.BusinessRules;

public static class OpportunityValidationRule
{
    public static void ValidateOpportunity(decimal amount, int probability, DateTime expectedCloseDate, OpportunityStatus status)
    {
        if (probability < 0 || probability > 100)
        {
            throw new BusinessRuleException("Probability must be between 0 and 100.");
        }

        if (status == OpportunityStatus.Open)
        {
            if (amount <= 0)
            {
                throw new BusinessRuleException("Opportunity Amount must be greater than 0.");
            }

            if (expectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                throw new BusinessRuleException("Expected Close Date cannot be in the past.");
            }
        }
    }
}
