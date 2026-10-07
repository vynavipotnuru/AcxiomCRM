using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;
    private readonly IFollowUpService _followUpService;
    private readonly IActivityService _activityService;

    public DashboardService(
        IApplicationDbContext context,
        IFollowUpService followUpService,
        IActivityService activityService)
    {
        _context = context;
        _followUpService = followUpService;
        _activityService = activityService;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        string? currentUserId,
        string? currentUserRole,
        string? dateFilter = null,
        CancellationToken ct = default)
    {
        var custQuery = _context.Customers.AsNoTracking().AsQueryable();
        var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
        var oppQuery = _context.Opportunities.AsNoTracking().AsQueryable();

        // Scope filter
        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            custQuery = custQuery.Where(c => c.OwnerId == currentUserId);
            leadQuery = leadQuery.Where(l => l.AssignedTo == currentUserId);
            oppQuery = oppQuery.Where(o => o.AssignedTo == currentUserId);
        }

        // Date filter
        DateTime? startDate = null;
        var now = DateTime.UtcNow;
        if (dateFilter == "Today")
        {
            startDate = now.Date;
        }
        else if (dateFilter == "ThisWeek")
        {
            startDate = now.Date.AddDays(-(int)now.DayOfWeek);
        }
        else if (dateFilter == "ThisMonth")
        {
            startDate = new DateTime(now.Year, now.Month, 1);
        }

        if (startDate.HasValue)
        {
            custQuery = custQuery.Where(c => c.CreatedDate >= startDate.Value);
            leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate.Value);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate.Value);
        }

        var totalCustomers = await custQuery.CountAsync(ct);
        var totalLeads = await leadQuery.CountAsync(ct);
        var openLeads = await leadQuery.CountAsync(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost, ct);

        var totalOpps = await oppQuery.CountAsync(ct);
        var openOpps = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Open, ct);
        var wonOpps = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Won, ct);
        var lostOpps = await oppQuery.CountAsync(o => o.Status == OpportunityStatus.Lost, ct);
        var totalPipeline = (decimal)(await oppQuery.Where(o => o.Status == OpportunityStatus.Open).SumAsync(o => (double?)o.Amount, ct) ?? 0.0);

        // Upcoming and Overdue follow-up counts
        var today = DateTime.UtcNow.Date;
        var followUpQuery = _context.FollowUps.AsNoTracking().AsQueryable();
        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            followUpQuery = followUpQuery.Where(f => f.AssignedTo == currentUserId);
        }

        var todayFollowUps = await followUpQuery.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date == today, ct);
        var overdueFollowUps = await followUpQuery.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date < today, ct);

        // Chart 1: Lead Status
        var leadStatusGroups = await leadQuery
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var leadChart = new ChartDataDto
        {
            Labels = new List<string> { "New", "Contacted", "Qualified", "Converted", "Lost" },
            Data = new List<decimal>
            {
                leadStatusGroups.FirstOrDefault(g => g.Status == LeadStatus.New)?.Count ?? 0,
                leadStatusGroups.FirstOrDefault(g => g.Status == LeadStatus.Contacted)?.Count ?? 0,
                leadStatusGroups.FirstOrDefault(g => g.Status == LeadStatus.Qualified)?.Count ?? 0,
                leadStatusGroups.FirstOrDefault(g => g.Status == LeadStatus.Converted)?.Count ?? 0,
                leadStatusGroups.FirstOrDefault(g => g.Status == LeadStatus.Lost)?.Count ?? 0
            },
            BackgroundColors = new List<string> { "#3b82f6", "#06b6d4", "#10b981", "#8b5cf6", "#ef4444" }
        };

        // Chart 2: Opportunity Pipeline by Stage
        var oppStageGroups = await oppQuery
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, Amount = (decimal)(g.Sum(x => (double?)x.Amount) ?? 0.0) })
            .ToListAsync(ct);

        var oppChart = new ChartDataDto
        {
            Labels = new List<string> { "Qualification", "Proposal", "Negotiation", "Won", "Lost" },
            Data = new List<decimal>
            {
                oppStageGroups.FirstOrDefault(g => g.Stage == OpportunityStage.Qualification)?.Amount ?? 0,
                oppStageGroups.FirstOrDefault(g => g.Stage == OpportunityStage.Proposal)?.Amount ?? 0,
                oppStageGroups.FirstOrDefault(g => g.Stage == OpportunityStage.Negotiation)?.Amount ?? 0,
                oppStageGroups.FirstOrDefault(g => g.Stage == OpportunityStage.Won)?.Amount ?? 0,
                oppStageGroups.FirstOrDefault(g => g.Stage == OpportunityStage.Lost)?.Amount ?? 0
            },
            BackgroundColors = new List<string> { "#6366f1", "#0284c7", "#f59e0b", "#10b981", "#94a3b8" }
        };

        // Chart 3: Monthly Sales / Won Totals (last 6 months)
        var monthlyLabels = new List<string>();
        var monthlyData = new List<decimal>();
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var monthName = targetMonth.ToString("MMM yyyy");
            monthlyLabels.Add(monthName);

            var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var monthlyWonTotal = (decimal)(await oppQuery
                .Where(o => o.Status == OpportunityStatus.Won && o.CreatedDate >= monthStart && o.CreatedDate < monthEnd)
                .SumAsync(o => (double?)o.Amount, ct) ?? 0.0);

            monthlyData.Add(monthlyWonTotal);
        }

        var salesChart = new ChartDataDto
        {
            Labels = monthlyLabels,
            Data = monthlyData,
            BackgroundColors = new List<string> { "#3b82f6", "#3b82f6", "#3b82f6", "#3b82f6", "#3b82f6", "#10b981" }
        };

        var recentActivities = await _activityService.GetRecentActivitiesAsync(currentUserId, currentUserRole, 5, ct);
        var upcomingFollowUps = await _followUpService.GetUpcomingFollowUpsAsync(currentUserId, currentUserRole, 5, ct);

        return new DashboardSummaryDto
        {
            TotalCustomers = totalCustomers,
            TotalLeads = totalLeads,
            OpenLeads = openLeads,
            TotalOpportunities = totalOpps,
            OpenOpportunities = openOpps,
            WonOpportunities = wonOpps,
            LostOpportunities = lostOpps,
            TotalPipelineValue = totalPipeline,
            TodayFollowUpsCount = todayFollowUps,
            OverdueFollowUpsCount = overdueFollowUps,
            LeadStatusChart = leadChart,
            OpportunityPipelineChart = oppChart,
            MonthlySalesChart = salesChart,
            RecentActivities = recentActivities,
            UpcomingFollowUps = upcomingFollowUps
        };
    }
}
