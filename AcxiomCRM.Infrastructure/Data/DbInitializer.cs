using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AcxiomCRM.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        try
        {
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = { "Admin", "Manager", "SalesExecutive" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole(role, $"{role} Role with dedicated permissions"));
                }
            }

            // 2. Seed Default Users
            var usersToSeed = new[]
            {
                new { Email = "admin@acxiom.com", Name = "Vynavi Potnuru", Role = "Admin", Phone = "9876543210" },
                new { Email = "manager@acxiom.com", Name = "Rajesh Kumar", Role = "Manager", Phone = "9876543211" },
                new { Email = "sales@acxiom.com", Name = "Neha Singh", Role = "SalesExecutive", Phone = "9876543212" }
            };

            var userMap = new Dictionary<string, string>(); // Role -> UserId

            foreach (var u in usersToSeed)
            {
                var existingUser = await userManager.FindByEmailAsync(u.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = u.Email,
                        Email = u.Email,
                        FullName = u.Name,
                        PhoneNumber = u.Phone,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(user, "Acxiom@2026!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, u.Role);
                        userMap[u.Role] = user.Id;
                    }
                }
                else
                {
                    userMap[u.Role] = existingUser.Id;
                }
            }

            var adminId = userMap.GetValueOrDefault("Admin");
            var managerId = userMap.GetValueOrDefault("Manager");
            var salesId = userMap.GetValueOrDefault("SalesExecutive");

            // 3. Seed Customers
            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new()
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "ABC Corp",
                        Email = "contact@abccorp.com",
                        Phone = "9876543210",
                        CompanyName = "ABC Corporation",
                        Address = "HITEC City, Phase 2",
                        City = "Hyderabad",
                        State = "Telangana",
                        Status = CustomerStatus.Active,
                        OwnerId = salesId,
                        CreatedBy = "Admin",
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        Notes = "Key enterprise account in IT services sector."
                    },
                    new()
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "XYZ Ltd",
                        Email = "info@xyz.com",
                        Phone = "9123456780",
                        CompanyName = "XYZ Limited",
                        Address = "Bandra Kurla Complex",
                        City = "Mumbai",
                        State = "Maharashtra",
                        Status = CustomerStatus.Active,
                        OwnerId = salesId,
                        CreatedBy = "Admin",
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        Notes = "Financial consulting firm expanding digital infrastructure."
                    },
                    new()
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "TechSoft",
                        Email = "sales@techsoft.com",
                        Phone = "9988776655",
                        CompanyName = "TechSoft Innovations",
                        Address = "Electronic City",
                        City = "Bangalore",
                        State = "Karnataka",
                        Status = CustomerStatus.Inactive,
                        OwnerId = managerId,
                        CreatedBy = "Manager",
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        Notes = "Software vendor undergoing organizational restructuring."
                    },
                    new()
                    {
                        CustomerCode = "CUST-1004",
                        CustomerName = "Global Inc",
                        Email = "hello@globalinc.com",
                        Phone = "9001122334",
                        CompanyName = "Global Enterprises Inc",
                        Address = "Cyber City",
                        City = "Gurugram",
                        State = "Haryana",
                        Status = CustomerStatus.Active,
                        OwnerId = salesId,
                        CreatedBy = "Admin",
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        Notes = "Multinational manufacturing client."
                    }
                };

                await context.Customers.AddRangeAsync(customers);
                await context.SaveChangesAsync();
            }

            // 4. Seed Leads
            if (!await context.Leads.AnyAsync())
            {
                var leads = new List<Lead>
                {
                    new()
                    {
                        LeadCode = "LEAD-1001",
                        LeadName = "Rahul Mehta",
                        Email = "rahul@techsoft.com",
                        Phone = "9876543213",
                        CompanyName = "TechSoft",
                        Source = "Website",
                        Status = LeadStatus.New,
                        Priority = Priority.High,
                        ExpectedValue = 500000m,
                        AssignedTo = salesId,
                        Notes = "Interested in enterprise CRM migration.",
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    },
                    new()
                    {
                        LeadCode = "LEAD-1002",
                        LeadName = "Priya Sharma",
                        Email = "priya@abc.com",
                        Phone = "9876543214",
                        CompanyName = "ABC Group",
                        Source = "Referral",
                        Status = LeadStatus.Qualified,
                        Priority = Priority.High,
                        ExpectedValue = 1000000m,
                        AssignedTo = managerId,
                        Notes = "Budget approved, evaluating software capabilities.",
                        CreatedDate = DateTime.UtcNow.AddDays(-8)
                    },
                    new()
                    {
                        LeadCode = "LEAD-1003",
                        LeadName = "Karan Singh",
                        Email = "karan@innovate.ltd",
                        Phone = "9876543215",
                        CompanyName = "Innovate Ltd",
                        Source = "Cold Call",
                        Status = LeadStatus.Contacted,
                        Priority = Priority.Medium,
                        ExpectedValue = 250000m,
                        AssignedTo = salesId,
                        Notes = "Requested formal proposal deck next week.",
                        CreatedDate = DateTime.UtcNow.AddDays(-12)
                    },
                    new()
                    {
                        LeadCode = "LEAD-1004",
                        LeadName = "Sneha Reddy",
                        Email = "sneha@globalinc.com",
                        Phone = "9876543216",
                        CompanyName = "Global Inc",
                        Source = "Webinar",
                        Status = LeadStatus.Converted,
                        Priority = Priority.High,
                        ExpectedValue = 800000m,
                        AssignedTo = salesId,
                        Notes = "Successfully converted to client account.",
                        CreatedDate = DateTime.UtcNow.AddDays(-25),
                        ConvertedDate = DateTime.UtcNow.AddDays(-10)
                    },
                    new()
                    {
                        LeadCode = "LEAD-1005",
                        LeadName = "Amit Kumar",
                        Email = "amit@nextgen.io",
                        Phone = "9876543217",
                        CompanyName = "NextGen Systems",
                        Source = "Website",
                        Status = LeadStatus.Lost,
                        Priority = Priority.Low,
                        ExpectedValue = 150000m,
                        AssignedTo = salesId,
                        Notes = "Postponed purchase to next fiscal year.",
                        CreatedDate = DateTime.UtcNow.AddDays(-30)
                    }
                };

                await context.Leads.AddRangeAsync(leads);
                await context.SaveChangesAsync();
            }

            // 5. Seed Opportunities
            if (!await context.Opportunities.AnyAsync())
            {
                var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
                var cust2 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1002");
                var cust3 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1004");

                var opportunities = new List<Opportunity>
                {
                    new()
                    {
                        OpportunityName = "CRM Deal - ABC Enterprise",
                        CustomerId = cust1?.CustomerId,
                        Amount = 500000m,
                        Stage = OpportunityStage.Proposal,
                        Probability = 60,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                        Status = OpportunityStatus.Open,
                        AssignedTo = salesId,
                        Notes = "Enterprise proposal submitted for 50 licenses.",
                        CreatedDate = DateTime.UtcNow.AddDays(-15)
                    },
                    new()
                    {
                        OpportunityName = "ERP Project Integration",
                        CustomerId = cust2?.CustomerId,
                        Amount = 1000000m,
                        Stage = OpportunityStage.Negotiation,
                        Probability = 80,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(15),
                        Status = OpportunityStatus.Open,
                        AssignedTo = managerId,
                        Notes = "Pricing negotiations in final legal review.",
                        CreatedDate = DateTime.UtcNow.AddDays(-20)
                    },
                    new()
                    {
                        OpportunityName = "Support & Maintenance Contract",
                        CustomerId = cust1?.CustomerId,
                        Amount = 250000m,
                        Stage = OpportunityStage.Qualification,
                        Probability = 40,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(45),
                        Status = OpportunityStatus.Open,
                        AssignedTo = salesId,
                        Notes = "Annual support contract discovery phase.",
                        CreatedDate = DateTime.UtcNow.AddDays(-7)
                    },
                    new()
                    {
                        OpportunityName = "Cloud Migration Services",
                        CustomerId = cust3?.CustomerId,
                        Amount = 800000m,
                        Stage = OpportunityStage.Won,
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-2),
                        Status = OpportunityStatus.Won,
                        AssignedTo = salesId,
                        Notes = "Contract signed and onboarding initiated.",
                        CreatedDate = DateTime.UtcNow.AddDays(-40)
                    }
                };

                await context.Opportunities.AddRangeAsync(opportunities);
                await context.SaveChangesAsync();
            }

            // 6. Seed Follow-ups
            if (!await context.FollowUps.AnyAsync())
            {
                var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
                var lead1 = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-1001");

                var followUps = new List<FollowUp>
                {
                    new()
                    {
                        CustomerId = cust1?.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(1),
                        FollowUpType = FollowUpType.Call,
                        Subject = "Follow up on Proposal review",
                        Remarks = "Discuss executive sign-off for CRM rollout.",
                        Status = FollowUpStatus.Planned,
                        AssignedTo = salesId,
                        CreatedDate = DateTime.UtcNow
                    },
                    new()
                    {
                        LeadId = lead1?.LeadId,
                        FollowUpDate = DateTime.UtcNow.AddDays(2),
                        FollowUpType = FollowUpType.Meeting,
                        Subject = "Product Demo for TechSoft team",
                        Remarks = "Showcase automation workflows and dashboard analytics.",
                        Status = FollowUpStatus.Planned,
                        AssignedTo = salesId,
                        CreatedDate = DateTime.UtcNow
                    },
                    new()
                    {
                        CustomerId = cust1?.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(-1),
                        FollowUpType = FollowUpType.Email,
                        Subject = "Sent updated commercial terms",
                        Remarks = "Delivered revision 2 of quotation.",
                        Status = FollowUpStatus.Completed,
                        AssignedTo = salesId,
                        CreatedDate = DateTime.UtcNow.AddDays(-3)
                    }
                };

                await context.FollowUps.AddRangeAsync(followUps);
                await context.SaveChangesAsync();
            }

            // 7. Seed Activities
            if (!await context.Activities.AnyAsync())
            {
                var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
                var activities = new List<Activity>
                {
                    new()
                    {
                        ActivityType = ActivityType.Call,
                        Subject = "Initial Introduction Call",
                        Description = "Discussed business requirements and CRM feature scope.",
                        ActivityDate = DateTime.UtcNow.AddDays(-14),
                        CustomerId = cust1?.CustomerId,
                        AssignedTo = salesId,
                        Status = "Completed"
                    },
                    new()
                    {
                        ActivityType = ActivityType.Meeting,
                        Subject = "Requirements Discovery Session",
                        Description = "Met with engineering and sales heads to align on requirements.",
                        ActivityDate = DateTime.UtcNow.AddDays(-7),
                        CustomerId = cust1?.CustomerId,
                        AssignedTo = salesId,
                        Status = "Completed"
                    }
                };

                await context.Activities.AddRangeAsync(activities);
                await context.SaveChangesAsync();
            }

            // 8. Seed Audit Log entries
            if (!await context.AuditLogs.AnyAsync())
            {
                var auditLogs = new List<AuditLog>
                {
                    new()
                    {
                        UserId = adminId,
                        UserName = "admin@acxiom.com",
                        Action = "SystemInit",
                        EntityName = "Database",
                        RecordId = "0",
                        CreatedDate = DateTime.UtcNow.AddDays(-30),
                        IpAddress = "127.0.0.1",
                        Result = "Success",
                        Details = "Initial system provisioning and baseline data seeded."
                    },
                    new()
                    {
                        UserId = adminId,
                        UserName = "admin@acxiom.com",
                        Action = "Login",
                        EntityName = "Authentication",
                        RecordId = adminId,
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        IpAddress = "127.0.0.1",
                        Result = "Success",
                        Details = "User admin@acxiom.com logged in successfully."
                    }
                };

                await context.AuditLogs.AddRangeAsync(auditLogs);
                await context.SaveChangesAsync();
            }

            logger.LogInformation("AcxiomCRM database seeded successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding AcxiomCRM database.");
            throw;
        }
    }
}
