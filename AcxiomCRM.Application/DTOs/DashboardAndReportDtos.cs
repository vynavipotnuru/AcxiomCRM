namespace AcxiomCRM.Application.DTOs;

public class DashboardSummaryDto
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public int TodayFollowUpsCount { get; set; }
    public int OverdueFollowUpsCount { get; set; }

    public ChartDataDto LeadStatusChart { get; set; } = new();
    public ChartDataDto OpportunityPipelineChart { get; set; } = new();
    public ChartDataDto MonthlySalesChart { get; set; } = new();

    public List<ActivityDto> RecentActivities { get; set; } = new();
    public List<FollowUpDto> UpcomingFollowUps { get; set; } = new();
}

public class ChartDataDto
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Data { get; set; } = new();
    public List<string> BackgroundColors { get; set; } = new();
}

public class CustomerReportDto
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class LeadReportDto
{
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Source { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ExpectedValue { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsConverted { get; set; }
}

public class PipelineReportDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class ConversionReportDto
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public int LostLeads { get; set; }
    public int OpenLeads { get; set; }
    public decimal ConversionRatePercentage => TotalLeads > 0 ? Math.Round((decimal)ConvertedLeads * 100m / TotalLeads, 1) : 0;
}

public class UserActivityReportDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int CustomersCount { get; set; }
    public int LeadsCount { get; set; }
    public int OpportunitiesCount { get; set; }
    public int ActivitiesCount { get; set; }
    public int FollowUpsCount { get; set; }
}
