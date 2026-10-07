using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using Xunit;

namespace AcxiomCRM.Tests.Validation;

public class BusinessValidationTests
{
    [Fact]
    public void Opportunity_AmountZeroOrNegative_ThrowsBusinessRuleException()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
            OpportunityValidationRule.ValidateOpportunity(
                amount: 0m,
                probability: 50,
                expectedCloseDate: DateTime.UtcNow.AddDays(10),
                status: OpportunityStatus.Open));

        Assert.Contains("Opportunity Amount must be greater than 0", ex.Message);
    }

    [Fact]
    public void Opportunity_ProbabilityOutOfRange_ThrowsBusinessRuleException()
    {
        // Act & Assert (Probability > 100)
        var exHigh = Assert.Throws<BusinessRuleException>(() =>
            OpportunityValidationRule.ValidateOpportunity(
                amount: 50000m,
                probability: 105,
                expectedCloseDate: DateTime.UtcNow.AddDays(10),
                status: OpportunityStatus.Open));

        Assert.Contains("Probability must be between 0 and 100", exHigh.Message);

        // Act & Assert (Probability < 0)
        var exLow = Assert.Throws<BusinessRuleException>(() =>
            OpportunityValidationRule.ValidateOpportunity(
                amount: 50000m,
                probability: -5,
                expectedCloseDate: DateTime.UtcNow.AddDays(10),
                status: OpportunityStatus.Open));

        Assert.Contains("Probability must be between 0 and 100", exLow.Message);
    }

    [Fact]
    public void Opportunity_ExpectedCloseDateInPast_ThrowsBusinessRuleException()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
            OpportunityValidationRule.ValidateOpportunity(
                amount: 50000m,
                probability: 50,
                expectedCloseDate: DateTime.UtcNow.AddDays(-2),
                status: OpportunityStatus.Open));

        Assert.Contains("Expected Close Date cannot be in the past", ex.Message);
    }

    [Fact]
    public void Opportunity_WeightedPipeline_IsCalculatedCorrectly()
    {
        // Arrange
        var opp = new Opportunity
        {
            Amount = 100000m,
            Probability = 60
        };

        // Act
        var weighted = opp.WeightedPipeline;

        // Assert: 100000 * 60 / 100 = 60000
        Assert.Equal(60000m, weighted);
    }

    [Fact]
    public void FollowUp_DateInPastForPlanned_ThrowsBusinessRuleException()
    {
        // Act & Assert
        var ex = Assert.Throws<BusinessRuleException>(() =>
            FollowUpValidationRule.ValidateFollowUpDate(
                followUpDate: DateTime.UtcNow.AddDays(-1),
                status: FollowUpStatus.Planned,
                isNew: true));

        Assert.Contains("Follow-up date cannot be earlier than today", ex.Message);
    }

    [Fact]
    public void FollowUp_DateTodayOrFuture_PassesValidation()
    {
        // Act & Assert (Does not throw)
        FollowUpValidationRule.ValidateFollowUpDate(
            followUpDate: DateTime.UtcNow.AddDays(1),
            status: FollowUpStatus.Planned,
            isNew: true);
    }

    [Fact]
    public void LeadStatusTransition_ValidPath_Succeeds()
    {
        // New -> Contacted
        Assert.True(LeadStatusTransitionRule.IsValidTransition(LeadStatus.New, LeadStatus.Contacted));
        // Contacted -> Qualified
        Assert.True(LeadStatusTransitionRule.IsValidTransition(LeadStatus.Contacted, LeadStatus.Qualified));
        // Qualified -> Converted
        Assert.True(LeadStatusTransitionRule.IsValidTransition(LeadStatus.Qualified, LeadStatus.Converted));
    }

    [Fact]
    public void LeadStatusTransition_InvalidPath_ThrowsBusinessRuleException()
    {
        // Converted is terminal -> cannot transition to Contacted or New
        Assert.False(LeadStatusTransitionRule.IsValidTransition(LeadStatus.Converted, LeadStatus.Contacted));

        var ex = Assert.Throws<BusinessRuleException>(() =>
            LeadStatusTransitionRule.ValidateTransition(LeadStatus.Converted, LeadStatus.Contacted));

        Assert.Contains("Invalid lead status transition", ex.Message);
    }
}
