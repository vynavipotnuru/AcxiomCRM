using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Application.Services;

public class FollowUpService : IFollowUpService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public FollowUpService(IApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<PagedResult<FollowUpDto>> GetFollowUpsAsync(
        string? searchTerm,
        FollowUpStatus? status,
        FollowUpType? type,
        string? currentUserId,
        string? currentUserRole,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .AsNoTracking()
            .AsQueryable();

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(f => f.AssignedTo == currentUserId);
        }

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(f => f.FollowUpType == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();
            query = query.Where(f =>
                f.Subject.ToLower().Contains(searchTerm) ||
                (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(searchTerm)) ||
                (f.Lead != null && f.Lead.LeadName.ToLower().Contains(searchTerm)) ||
                (f.Opportunity != null && f.Opportunity.OpportunityName.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(f => f.FollowUpDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                OpportunityId = f.OpportunityId,
                OpportunityName = f.Opportunity != null ? f.Opportunity.OpportunityName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedTo = f.AssignedTo,
                CreatedDate = f.CreatedDate
            })
            .ToListAsync(ct);

        return new PagedResult<FollowUpDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<FollowUpDto?> GetFollowUpByIdAsync(
        int id,
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var f = await _context.FollowUps
            .Include(x => x.Customer)
            .Include(x => x.Lead)
            .Include(x => x.Opportunity)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FollowUpId == id, ct);

        if (f == null) return null;

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId) && f.AssignedTo != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this follow-up.");
        }

        return new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer?.CustomerName,
            LeadId = f.LeadId,
            LeadName = f.Lead?.LeadName,
            OpportunityId = f.OpportunityId,
            OpportunityName = f.Opportunity?.OpportunityName,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Subject = f.Subject,
            Remarks = f.Remarks,
            Status = f.Status,
            AssignedTo = f.AssignedTo,
            CreatedDate = f.CreatedDate
        };
    }

    public async Task<FollowUpDto> CreateFollowUpAsync(
        CreateFollowUpDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        FollowUpValidationRule.ValidateFollowUpDate(dto.FollowUpDate, dto.Status, isNew: true);

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = dto.FollowUpType,
            Subject = dto.Subject.Trim(),
            Remarks = dto.Remarks?.Trim(),
            Status = dto.Status,
            AssignedTo = !string.IsNullOrEmpty(dto.AssignedTo) ? dto.AssignedTo : currentUserId,
            CreatedDate = DateTime.UtcNow
        };

        await _context.FollowUps.AddAsync(followUp, ct);
        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Create",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            null,
            $"{followUp.Subject} on {followUp.FollowUpDate:yyyy-MM-dd}",
            "Success",
            $"Follow-up created of type {followUp.FollowUpType}",
            ipAddress);

        return new FollowUpDto
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Subject = followUp.Subject,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedTo = followUp.AssignedTo,
            CreatedDate = followUp.CreatedDate
        };
    }

    public async Task<FollowUpDto> UpdateFollowUpAsync(
        UpdateFollowUpDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == dto.FollowUpId, ct);
        if (followUp == null) throw new KeyNotFoundException($"Follow-up with ID {dto.FollowUpId} not found.");

        if (dto.Status == FollowUpStatus.Planned && dto.FollowUpDate.Date != followUp.FollowUpDate.Date)
        {
            FollowUpValidationRule.ValidateFollowUpDate(dto.FollowUpDate, dto.Status, isNew: false);
        }

        var oldState = $"{followUp.Subject}, {followUp.FollowUpDate:yyyy-MM-dd}, {followUp.Status}";

        followUp.CustomerId = dto.CustomerId;
        followUp.LeadId = dto.LeadId;
        followUp.OpportunityId = dto.OpportunityId;
        followUp.FollowUpDate = dto.FollowUpDate;
        followUp.FollowUpType = dto.FollowUpType;
        followUp.Subject = dto.Subject.Trim();
        followUp.Remarks = dto.Remarks?.Trim();
        followUp.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.AssignedTo)) followUp.AssignedTo = dto.AssignedTo;

        await _context.SaveChangesAsync(ct);

        var newState = $"{followUp.Subject}, {followUp.FollowUpDate:yyyy-MM-dd}, {followUp.Status}";

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Update",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            oldState,
            newState,
            "Success",
            $"Follow-up updated: {followUp.Subject}",
            ipAddress);

        return new FollowUpDto
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Subject = followUp.Subject,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedTo = followUp.AssignedTo,
            CreatedDate = followUp.CreatedDate
        };
    }

    public async Task<bool> MarkCompleteAsync(int id, string? remarks, string? currentUserId, string? ipAddress, CancellationToken ct = default)
    {
        var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id, ct);
        if (followUp == null) return false;

        followUp.Status = FollowUpStatus.Completed;
        if (!string.IsNullOrEmpty(remarks))
        {
            followUp.Remarks = string.IsNullOrEmpty(followUp.Remarks) ? remarks : $"{followUp.Remarks}\n[Completion Note]: {remarks}";
        }

        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Complete",
            "FollowUp",
            id.ToString(),
            FollowUpStatus.Planned.ToString(),
            FollowUpStatus.Completed.ToString(),
            "Success",
            $"Follow-up marked completed: {followUp.Subject}",
            ipAddress);

        return true;
    }

    public async Task<bool> RescheduleAsync(int id, DateTime newDate, string? remarks, string? currentUserId, string? ipAddress, CancellationToken ct = default)
    {
        var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id, ct);
        if (followUp == null) return false;

        FollowUpValidationRule.ValidateFollowUpDate(newDate, FollowUpStatus.Planned, isNew: false);

        var oldDate = followUp.FollowUpDate.ToString("yyyy-MM-dd HH:mm");
        followUp.FollowUpDate = newDate;
        followUp.Status = FollowUpStatus.Planned;
        if (!string.IsNullOrEmpty(remarks))
        {
            followUp.Remarks = string.IsNullOrEmpty(followUp.Remarks) ? remarks : $"{followUp.Remarks}\n[Reschedule Note]: {remarks}";
        }

        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Reschedule",
            "FollowUp",
            id.ToString(),
            oldDate,
            newDate.ToString("yyyy-MM-dd HH:mm"),
            "Success",
            $"Follow-up rescheduled: {followUp.Subject}",
            ipAddress);

        return true;
    }

    public async Task<bool> CancelAsync(int id, string? remarks, string? currentUserId, string? ipAddress, CancellationToken ct = default)
    {
        var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id, ct);
        if (followUp == null) return false;

        followUp.Status = FollowUpStatus.Cancelled;
        if (!string.IsNullOrEmpty(remarks))
        {
            followUp.Remarks = string.IsNullOrEmpty(followUp.Remarks) ? remarks : $"{followUp.Remarks}\n[Cancellation Note]: {remarks}";
        }

        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Cancel",
            "FollowUp",
            id.ToString(),
            FollowUpStatus.Planned.ToString(),
            FollowUpStatus.Cancelled.ToString(),
            "Success",
            $"Follow-up cancelled: {followUp.Subject}",
            ipAddress);

        return true;
    }

    public async Task<List<FollowUpDto>> GetUpcomingFollowUpsAsync(
        string? currentUserId,
        string? currentUserRole,
        int count = 5,
        CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .AsNoTracking()
            .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date >= today);

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(f => f.AssignedTo == currentUserId);
        }

        return await query
            .OrderBy(f => f.FollowUpDate)
            .Take(count)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Status = f.Status,
                AssignedTo = f.AssignedTo
            })
            .ToListAsync(ct);
    }

    public async Task<List<FollowUpDto>> GetOverdueFollowUpsAsync(
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .AsNoTracking()
            .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate.Date < today);

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(f => f.AssignedTo == currentUserId);
        }

        return await query
            .OrderBy(f => f.FollowUpDate)
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
                LeadId = f.LeadId,
                LeadName = f.Lead != null ? f.Lead.LeadName : null,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Status = f.Status,
                AssignedTo = f.AssignedTo
            })
            .ToListAsync(ct);
    }
}
