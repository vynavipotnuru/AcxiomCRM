using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AcxiomCRM.Tests.Unit;

public class CustomerServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateCustomer_UniqueEmailAndPhone_Succeeds()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mockAudit = new Mock<IAuditLogService>();
        var service = new CustomerService(context, mockAudit.Object);

        var dto = new CreateCustomerDto
        {
            CustomerName = "Acme Corp",
            Email = "contact@acme.com",
            Phone = "9876543210",
            CompanyName = "Acme Ltd",
            Status = CustomerStatus.Active
        };

        // Act
        var result = await service.CreateCustomerAsync(dto, "user-1", "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Acme Corp", result.CustomerName);
        Assert.Equal("contact@acme.com", result.Email);
        Assert.StartsWith("CUST-", result.CustomerCode);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_ThrowsBusinessRuleException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mockAudit = new Mock<IAuditLogService>();
        var service = new CustomerService(context, mockAudit.Object);

        var first = new CreateCustomerDto
        {
            CustomerName = "First Client",
            Email = "duplicate@acme.com",
            Phone = "9876543210"
        };
        await service.CreateCustomerAsync(first, "user-1", "127.0.0.1");

        var duplicate = new CreateCustomerDto
        {
            CustomerName = "Second Client",
            Email = "duplicate@acme.com",
            Phone = "9876543211"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateCustomerAsync(duplicate, "user-1", "127.0.0.1"));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateCustomerAsync(duplicate, "user-1", "127.0.0.1"));
    }

    [Fact]
    public async Task CreateCustomer_DuplicatePhone_ThrowsBusinessRuleException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mockAudit = new Mock<IAuditLogService>();
        var service = new CustomerService(context, mockAudit.Object);

        var first = new CreateCustomerDto
        {
            CustomerName = "Client A",
            Email = "a@acme.com",
            Phone = "9876543210"
        };
        await service.CreateCustomerAsync(first, "user-1", "127.0.0.1");

        var duplicatePhone = new CreateCustomerDto
        {
            CustomerName = "Client B",
            Email = "b@acme.com",
            Phone = "9876543210"
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateCustomerAsync(duplicatePhone, "user-1", "127.0.0.1"));
    }
}
