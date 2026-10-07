using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Application.Services;

public class ActivityService : IActivityService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public ActivityService(IApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<List<ActivityDto>> GetRecentActivitiesAsync(
        string? currentUserId,
        string? currentUserRole,
        int count = 10,
        CancellationToken ct = default)
    {
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .AsNoTracking()
            .AsQueryable();

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(a => a.AssignedTo == currentUserId);
        }

        return await query
            .OrderByDescending(a => a.ActivityDate)
            .Take(count)
            .Select(a => new ActivityDto
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                CustomerId = a.CustomerId,
                CustomerName = a.Customer != null ? a.Customer.CustomerName : null,
                LeadId = a.LeadId,
                LeadName = a.Lead != null ? a.Lead.LeadName : null,
                AssignedTo = a.AssignedTo,
                Status = a.Status,
                CreatedDate = a.CreatedDate
            })
            .ToListAsync(ct);
    }

    public async Task<ActivityDto> CreateActivityAsync(
        CreateActivityDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var activity = new Activity
        {
            ActivityType = dto.ActivityType,
            Subject = dto.Subject.Trim(),
            Description = dto.Description?.Trim(),
            ActivityDate = dto.ActivityDate,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            AssignedTo = !string.IsNullOrEmpty(dto.AssignedTo) ? dto.AssignedTo : currentUserId,
            Status = dto.Status,
            CreatedDate = DateTime.UtcNow
        };

        await _context.Activities.AddAsync(activity, ct);
        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Create",
            "Activity",
            activity.ActivityId.ToString(),
            null,
            $"{activity.ActivityType}: {activity.Subject}",
            "Success",
            $"Logged CRM activity",
            ipAddress);

        return new ActivityDto
        {
            ActivityId = activity.ActivityId,
            ActivityType = activity.ActivityType,
            Subject = activity.Subject,
            Description = activity.Description,
            ActivityDate = activity.ActivityDate,
            CustomerId = activity.CustomerId,
            LeadId = activity.LeadId,
            AssignedTo = activity.AssignedTo,
            Status = activity.Status,
            CreatedDate = activity.CreatedDate
        };
    }

    public async Task<List<ActivityDto>> GetActivitiesForCustomerAsync(int customerId, CancellationToken ct = default)
    {
        return await _context.Activities
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.ActivityDate)
            .Select(a => new ActivityDto
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                CustomerId = a.CustomerId,
                Status = a.Status
            })
            .ToListAsync(ct);
    }

    public async Task<List<ActivityDto>> GetActivitiesForLeadAsync(int leadId, CancellationToken ct = default)
    {
        return await _context.Activities
            .AsNoTracking()
            .Where(a => a.LeadId == leadId)
            .OrderByDescending(a => a.ActivityDate)
            .Select(a => new ActivityDto
            {
                ActivityId = a.ActivityId,
                ActivityType = a.ActivityType,
                Subject = a.Subject,
                Description = a.Description,
                ActivityDate = a.ActivityDate,
                LeadId = a.LeadId,
                Status = a.Status
            })
            .ToListAsync(ct);
    }
}
