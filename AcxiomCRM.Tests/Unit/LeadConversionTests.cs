using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AcxiomCRM.Tests.Unit;

public class LeadConversionTests
{
    private ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ConvertLead_QualifiedLead_CreatesCustomerAndOpportunity()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mockAudit = new Mock<IAuditLogService>();
        var service = new LeadService(context, mockAudit.Object);

        var lead = new Lead
        {
            LeadCode = "LEAD-1001",
            LeadName = "Vikram Sharma",
            Email = "vikram@enterprise.com",
            Phone = "9876543219",
            CompanyName = "Enterprise Tech",
            Status = LeadStatus.Qualified,
            ExpectedValue = 750000m
        };
        await context.Leads.AddAsync(lead);
        await context.SaveChangesAsync();

        var convertDto = new ConvertLeadDto
        {
            LeadId = lead.LeadId,
            CustomerName = "Vikram Sharma",
            CompanyName = "Enterprise Tech",
            Email = "vikram@enterprise.com",
            Phone = "9876543219",
            CreateOpportunity = true,
            OpportunityName = "Enterprise ERP Expansion",
            OpportunityAmount = 750000m,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var (customer, opportunity) = await service.ConvertLeadAsync(convertDto, "user-sales-1", "127.0.0.1");

        // Assert
        Assert.NotNull(customer);
        Assert.Equal("Vikram Sharma", customer.CustomerName);
        Assert.NotNull(opportunity);
        Assert.Equal(750000m, opportunity.Amount);

        // Verify lead is updated to Converted
        var updatedLead = await context.Leads.FindAsync(lead.LeadId);
        Assert.Equal(LeadStatus.Converted, updatedLead!.Status);
        Assert.NotNull(updatedLead.ConvertedDate);
        Assert.Equal(customer.CustomerId, updatedLead.ConvertedCustomerId);
    }

    [Fact]
    public async Task ConvertLead_AlreadyConvertedLead_ThrowsBusinessRuleException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mockAudit = new Mock<IAuditLogService>();
        var service = new LeadService(context, mockAudit.Object);

        var lead = new Lead
        {
            LeadCode = "LEAD-1002",
            LeadName = "Already Done",
            Email = "done@test.com",
            Phone = "9112233445",
            Status = LeadStatus.Converted
        };
        await context.Leads.AddAsync(lead);
        await context.SaveChangesAsync();

        var convertDto = new ConvertLeadDto
        {
            LeadId = lead.LeadId,
            CustomerName = "Done Customer",
            Email = "done@test.com",
            Phone = "9112233445"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.ConvertLeadAsync(convertDto, "user-1", "127.0.0.1"));

        Assert.Contains("already been converted", ex.Message);
    }
}
