using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Application.Services;

public class ReportService : IReportService
{
    private readonly IApplicationDbContext _context;

    public ReportService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustomerReportDto>> GetCustomerReportAsync(
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(c => c.OwnerId == currentUserId);
        }

        return await query
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new CustomerReportDto
            {
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Status = c.Status.ToString(),
                OwnerName = c.OwnerId,
                CreatedDate = c.CreatedDate
            })
            .ToListAsync(ct);
    }

    public async Task<List<LeadReportDto>> GetLeadReportAsync(
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var query = _context.Leads.AsNoTracking().AsQueryable();

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(l => l.AssignedTo == currentUserId);
        }

        return await query
            .OrderByDescending(l => l.CreatedDate)
            .Select(l => new LeadReportDto
            {
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status.ToString(),
                ExpectedValue = l.ExpectedValue,
                AssignedToName = l.AssignedTo,
                CreatedDate = l.CreatedDate,
                IsConverted = l.Status == LeadStatus.Converted
            })
            .ToListAsync(ct);
    }

    public async Task<List<PipelineReportDto>> GetPipelineReportAsync(
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var query = _context.Opportunities.AsNoTracking().AsQueryable();

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(o => o.AssignedTo == currentUserId);
        }

        var stages = Enum.GetValues<OpportunityStage>();
        var results = new List<PipelineReportDto>();

        foreach (var stage in stages)
        {
            var oppsInStage = await query.Where(o => o.Stage == stage).ToListAsync(ct);
            var totalAmt = oppsInStage.Sum(o => o.Amount);
            var weightedAmt = oppsInStage.Sum(o => o.WeightedPipeline);

            results.Add(new PipelineReportDto
            {
                Stage = stage.ToString(),
                Count = oppsInStage.Count,
                TotalAmount = totalAmt,
                WeightedAmount = weightedAmt
            });
        }

        return results;
    }

    public async Task<ConversionReportDto> GetConversionReportAsync(
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var query = _context.Leads.AsNoTracking().AsQueryable();

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(l => l.AssignedTo == currentUserId);
        }

        var total = await query.CountAsync(ct);
        var converted = await query.CountAsync(l => l.Status == LeadStatus.Converted, ct);
        var lost = await query.CountAsync(l => l.Status == LeadStatus.Lost, ct);
        var open = total - converted - lost;

        return new ConversionReportDto
        {
            TotalLeads = total,
            ConvertedLeads = converted,
            LostLeads = lost,
            OpenLeads = open
        };
    }

    public async Task<List<UserActivityReportDto>> GetUserActivityReportAsync(CancellationToken ct = default)
    {
        // Aggregate across customers, leads, opportunities, activities and followups
        var auditLogs = await _context.AuditLogs.AsNoTracking().ToListAsync(ct);

        var userGroups = auditLogs
            .Where(a => !string.IsNullOrEmpty(a.UserId) && a.UserId != "0")
            .GroupBy(a => new { a.UserId, a.UserName })
            .Select(g => new UserActivityReportDto
            {
                UserId = g.Key.UserId ?? string.Empty,
                UserName = g.Key.UserName ?? g.Key.UserId ?? "Unknown",
                Role = "User",
                CustomersCount = g.Count(x => x.EntityName == "Customer"),
                LeadsCount = g.Count(x => x.EntityName == "Lead"),
                OpportunitiesCount = g.Count(x => x.EntityName == "Opportunity"),
                FollowUpsCount = g.Count(x => x.EntityName == "FollowUp"),
                ActivitiesCount = g.Count(x => x.EntityName == "Activity")
            })
            .ToList();

        return userGroups;
    }
}
