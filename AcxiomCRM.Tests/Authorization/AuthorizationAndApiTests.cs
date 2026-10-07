using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Application.Services;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Data;
using AcxiomCRM.Web.Controllers.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AcxiomCRM.Tests.Authorization;

public class AuthorizationAndApiTests
{
    private ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetCustomers_SalesExecutive_OnlyReturnsAssignedCustomers()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var mockAudit = new Mock<IAuditLogService>();
        var service = new CustomerService(context, mockAudit.Object);

        // Add 2 customers assigned to sales-1, and 1 assigned to sales-2
        await context.Customers.AddRangeAsync(
            new Customer { CustomerCode = "C1", CustomerName = "Cust 1", Email = "c1@test.com", Phone = "1111111111", OwnerId = "sales-1" },
            new Customer { CustomerCode = "C2", CustomerName = "Cust 2", Email = "c2@test.com", Phone = "2222222222", OwnerId = "sales-1" },
            new Customer { CustomerCode = "C3", CustomerName = "Cust 3", Email = "c3@test.com", Phone = "3333333333", OwnerId = "sales-2" }
        );
        await context.SaveChangesAsync();

        // Act: Request as SalesExecutive
        var salesResult = await service.GetCustomersAsync(
            searchTerm: null,
            status: null,
            currentUserId: "sales-1",
            currentUserRole: "SalesExecutive");

        // Assert: Only 2 customers returned for sales-1
        Assert.Equal(2, salesResult.TotalCount);
        Assert.All(salesResult.Items, c => Assert.Equal("sales-1", c.OwnerId));

        // Act: Request as Admin
        var adminResult = await service.GetCustomersAsync(
            searchTerm: null,
            status: null,
            currentUserId: "admin-1",
            currentUserRole: "Admin");

        // Assert: All 3 customers returned for Admin
        Assert.Equal(3, adminResult.TotalCount);
    }

    [Fact]
    public async Task CustomerApi_GetCustomerById_ReturnsOkWithDto()
    {
        // Arrange
        var mockService = new Mock<ICustomerService>();
        mockService.Setup(s => s.GetCustomerByIdAsync(1, It.IsAny<string?>(), It.IsAny<string?>(), default))
            .ReturnsAsync(new CustomerDto
            {
                CustomerId = 1,
                CustomerCode = "CUST-1001",
                CustomerName = "Test Client",
                Email = "test@client.com"
            });

        var controller = new CustomersApiController(mockService.Object);

        // Act
        var result = await controller.GetCustomerById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<CustomerDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("CUST-1001", response.Data?.CustomerCode);
    }

    [Fact]
    public async Task CustomerApi_GetNonExistentCustomer_ReturnsNotFound()
    {
        // Arrange
        var mockService = new Mock<ICustomerService>();
        mockService.Setup(s => s.GetCustomerByIdAsync(999, It.IsAny<string?>(), It.IsAny<string?>(), default))
            .ReturnsAsync((CustomerDto?)null);

        var controller = new CustomersApiController(mockService.Object);

        // Act
        var result = await controller.GetCustomerById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
