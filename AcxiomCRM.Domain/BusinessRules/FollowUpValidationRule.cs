using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.BusinessRules;

public static class FollowUpValidationRule
{
    public static void ValidateFollowUpDate(DateTime followUpDate, FollowUpStatus status, bool isNew = false)
    {
        if (isNew || status == FollowUpStatus.Planned)
        {
            if (followUpDate.Date < DateTime.UtcNow.Date)
            {
                throw new BusinessRuleException("Follow-up date cannot be earlier than today.");
            }
        }
    }
}
