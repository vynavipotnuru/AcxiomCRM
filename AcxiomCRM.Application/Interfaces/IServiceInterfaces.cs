using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(string? searchTerm, CustomerStatus? status, string? currentUserId, string? currentUserRole, int pageIndex = 1, int pageSize = 10, CancellationToken ct = default);
    Task<CustomerDto?> GetCustomerByIdAsync(int id, string? currentUserId, string? currentUserRole, CancellationToken ct = default);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<CustomerDto> UpdateCustomerAsync(UpdateCustomerDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<bool> DeactivateCustomerAsync(int id, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<bool> DeleteCustomerAsync(int id, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<List<CustomerDto>> GetAllActiveCustomersAsync(string? currentUserId, string? currentUserRole, CancellationToken ct = default);
}

public interface ILeadService
{
    Task<PagedResult<LeadDto>> GetLeadsAsync(string? searchTerm, LeadStatus? status, Priority? priority, string? currentUserId, string? currentUserRole, int pageIndex = 1, int pageSize = 10, CancellationToken ct = default);
    Task<LeadDto?> GetLeadByIdAsync(int id, string? currentUserId, string? currentUserRole, CancellationToken ct = default);
    Task<LeadDto> CreateLeadAsync(CreateLeadDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<LeadDto> UpdateLeadAsync(UpdateLeadDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<LeadDto> UpdateLeadStatusAsync(int leadId, LeadStatus newStatus, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<(CustomerDto Customer, OpportunityDto? Opportunity)> ConvertLeadAsync(ConvertLeadDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<bool> DeleteLeadAsync(int id, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<List<LeadDto>> GetAllActiveLeadsAsync(string? currentUserId, string? currentUserRole, CancellationToken ct = default);
}

public interface IOpportunityService
{
    Task<PagedResult<OpportunityDto>> GetOpportunitiesAsync(string? searchTerm, OpportunityStage? stage, OpportunityStatus? status, string? currentUserId, string? currentUserRole, int pageIndex = 1, int pageSize = 10, CancellationToken ct = default);
    Task<OpportunityDto?> GetOpportunityByIdAsync(int id, string? currentUserId, string? currentUserRole, CancellationToken ct = default);
    Task<OpportunityDto> CreateOpportunityAsync(CreateOpportunityDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<OpportunityDto> UpdateOpportunityAsync(UpdateOpportunityDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<OpportunityDto> UpdateOpportunityStageAsync(int id, OpportunityStage newStage, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<bool> DeleteOpportunityAsync(int id, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<List<OpportunityDto>> GetAllOpportunitiesForPipelineAsync(string? currentUserId, string? currentUserRole, CancellationToken ct = default);
}

public interface IFollowUpService
{
    Task<PagedResult<FollowUpDto>> GetFollowUpsAsync(string? searchTerm, FollowUpStatus? status, FollowUpType? type, string? currentUserId, string? currentUserRole, int pageIndex = 1, int pageSize = 10, CancellationToken ct = default);
    Task<FollowUpDto?> GetFollowUpByIdAsync(int id, string? currentUserId, string? currentUserRole, CancellationToken ct = default);
    Task<FollowUpDto> CreateFollowUpAsync(CreateFollowUpDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<FollowUpDto> UpdateFollowUpAsync(UpdateFollowUpDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<bool> MarkCompleteAsync(int id, string? remarks, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<bool> RescheduleAsync(int id, DateTime newDate, string? remarks, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<bool> CancelAsync(int id, string? remarks, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<List<FollowUpDto>> GetUpcomingFollowUpsAsync(string? currentUserId, string? currentUserRole, int count = 5, CancellationToken ct = default);
    Task<List<FollowUpDto>> GetOverdueFollowUpsAsync(string? currentUserId, string? currentUserRole, CancellationToken ct = default);
}

public interface IActivityService
{
    Task<List<ActivityDto>> GetRecentActivitiesAsync(string? currentUserId, string? currentUserRole, int count = 10, CancellationToken ct = default);
    Task<ActivityDto> CreateActivityAsync(CreateActivityDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<List<ActivityDto>> GetActivitiesForCustomerAsync(int customerId, CancellationToken ct = default);
    Task<List<ActivityDto>> GetActivitiesForLeadAsync(int leadId, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(string? currentUserId, string? currentUserRole, string? dateFilter = null, CancellationToken ct = default);
}

public interface IReportService
{
    Task<List<CustomerReportDto>> GetCustomerReportAsync(string? currentUserId, string? currentUserRole, CancellationToken ct = default);
    Task<List<LeadReportDto>> GetLeadReportAsync(string? currentUserId, string? currentUserRole, CancellationToken ct = default);
    Task<List<PipelineReportDto>> GetPipelineReportAsync(string? currentUserId, string? currentUserRole, CancellationToken ct = default);
    Task<ConversionReportDto> GetConversionReportAsync(string? currentUserId, string? currentUserRole, CancellationToken ct = default);
    Task<List<UserActivityReportDto>> GetUserActivityReportAsync(CancellationToken ct = default);
}

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<UserDto?> GetUserByIdAsync(string id, CancellationToken ct = default);
    Task<(bool Success, string? Error)> CreateUserAsync(CreateUserDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<(bool Success, string? Error)> UpdateUserAsync(EditUserDto dto, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<bool> ToggleUserStatusAsync(string id, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(string userId, string newPassword, string? currentUserId, string? ipAddress, CancellationToken ct = default);
    Task<List<RoleDto>> GetAllRolesAsync(CancellationToken ct = default);
}

public interface IAuditLogService
{
    Task LogAsync(string? userId, string? userName, string action, string entityName, string? recordId, string? oldValue, string? newValue, string result, string? details, string? ipAddress);
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(string? searchTerm, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int pageIndex = 1, int pageSize = 15, CancellationToken ct = default);
}
