using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AcxiomCRM.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IApplicationDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        string? userId,
        string? userName,
        string action,
        string entityName,
        string? recordId,
        string? oldValue,
        string? newValue,
        string result,
        string? details,
        string? ipAddress)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                Result = result,
                Details = details,
                IpAddress = ipAddress ?? "127.0.0.1",
                CreatedDate = DateTime.UtcNow
            };

            await _context.AuditLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Do not fail user transactions if audit log fails, but log error
            _logger.LogError(ex, "Failed to persist audit log entry for entity {Entity} with action {Action}", entityName, action);
        }
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        string? searchTerm,
        string? entityName,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        int pageIndex = 1,
        int pageSize = 15,
        CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();
            query = query.Where(a =>
                (a.UserName != null && a.UserName.ToLower().Contains(searchTerm)) ||
                (a.Details != null && a.Details.ToLower().Contains(searchTerm)) ||
                (a.RecordId != null && a.RecordId.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(entityName) && entityName != "All")
        {
            query = query.Where(a => a.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(action) && action != "All")
        {
            query = query.Where(a => a.Action == action);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreatedDate <= endOfDay);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                AuditLogId = a.AuditLogId,
                UserId = a.UserId,
                UserName = a.UserName,
                Action = a.Action,
                EntityName = a.EntityName,
                RecordId = a.RecordId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                CreatedDate = a.CreatedDate,
                IpAddress = a.IpAddress,
                Result = a.Result,
                Details = a.Details
            })
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
}
