using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Application.Services;

public class OpportunityService : IOpportunityService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public OpportunityService(IApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<PagedResult<OpportunityDto>> GetOpportunitiesAsync(
        string? searchTerm,
        OpportunityStage? stage,
        OpportunityStatus? status,
        string? currentUserId,
        string? currentUserRole,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .AsNoTracking()
            .AsQueryable();

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(o => o.AssignedTo == currentUserId);
        }

        if (stage.HasValue)
        {
            query = query.Where(o => o.Stage == stage.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();
            query = query.Where(o =>
                o.OpportunityName.ToLower().Contains(searchTerm) ||
                (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.CreatedDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedTo = o.AssignedTo,
                Notes = o.Notes,
                CreatedDate = o.CreatedDate
            })
            .ToListAsync(ct);

        return new PagedResult<OpportunityDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<OpportunityDto?> GetOpportunityByIdAsync(
        int id,
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var o = await _context.Opportunities
            .Include(x => x.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OpportunityId == id, ct);

        if (o == null) return null;

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId) && o.AssignedTo != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this opportunity.");
        }

        return new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            AssignedTo = o.AssignedTo,
            Notes = o.Notes,
            CreatedDate = o.CreatedDate
        };
    }

    public async Task<OpportunityDto> CreateOpportunityAsync(
        CreateOpportunityDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        OpportunityValidationRule.ValidateOpportunity(dto.Amount, dto.Probability, dto.ExpectedCloseDate, dto.Status);

        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName.Trim(),
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Status,
            AssignedTo = !string.IsNullOrEmpty(dto.AssignedTo) ? dto.AssignedTo : currentUserId,
            Notes = dto.Notes?.Trim(),
            CreatedDate = DateTime.UtcNow
        };

        if (opp.Stage == OpportunityStage.Won) opp.Status = OpportunityStatus.Won;
        else if (opp.Stage == OpportunityStage.Lost) opp.Status = OpportunityStatus.Lost;

        await _context.Opportunities.AddAsync(opp, ct);
        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Create",
            "Opportunity",
            opp.OpportunityId.ToString(),
            null,
            $"{opp.OpportunityName}: ₹{opp.Amount}",
            "Success",
            $"Opportunity created with stage {opp.Stage}",
            ipAddress);

        return new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            AssignedTo = opp.AssignedTo,
            Notes = opp.Notes,
            CreatedDate = opp.CreatedDate
        };
    }

    public async Task<OpportunityDto> UpdateOpportunityAsync(
        UpdateOpportunityDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var opp = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == dto.OpportunityId, ct);
        if (opp == null) throw new KeyNotFoundException($"Opportunity with ID {dto.OpportunityId} not found.");

        OpportunityValidationRule.ValidateOpportunity(dto.Amount, dto.Probability, dto.ExpectedCloseDate, dto.Status);

        var oldState = $"{opp.OpportunityName}, {opp.Amount}, {opp.Stage}, {opp.Probability}%";

        opp.OpportunityName = dto.OpportunityName.Trim();
        opp.CustomerId = dto.CustomerId;
        opp.LeadId = dto.LeadId;
        opp.Amount = dto.Amount;
        opp.Stage = dto.Stage;
        opp.Probability = dto.Probability;
        opp.ExpectedCloseDate = dto.ExpectedCloseDate;
        opp.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.AssignedTo)) opp.AssignedTo = dto.AssignedTo;
        opp.Notes = dto.Notes?.Trim();

        if (opp.Stage == OpportunityStage.Won) opp.Status = OpportunityStatus.Won;
        else if (opp.Stage == OpportunityStage.Lost) opp.Status = OpportunityStatus.Lost;

        await _context.SaveChangesAsync(ct);

        var newState = $"{opp.OpportunityName}, {opp.Amount}, {opp.Stage}, {opp.Probability}%";

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Update",
            "Opportunity",
            opp.OpportunityId.ToString(),
            oldState,
            newState,
            "Success",
            $"Opportunity updated: {opp.OpportunityName}",
            ipAddress);

        return new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            AssignedTo = opp.AssignedTo,
            Notes = opp.Notes,
            CreatedDate = opp.CreatedDate
        };
    }

    public async Task<OpportunityDto> UpdateOpportunityStageAsync(
        int id,
        OpportunityStage newStage,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var opp = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id, ct);
        if (opp == null) throw new KeyNotFoundException($"Opportunity with ID {id} not found.");

        var oldStage = opp.Stage.ToString();
        opp.Stage = newStage;
        if (newStage == OpportunityStage.Won)
        {
            opp.Status = OpportunityStatus.Won;
            opp.Probability = 100;
        }
        else if (newStage == OpportunityStage.Lost)
        {
            opp.Status = OpportunityStatus.Lost;
            opp.Probability = 0;
        }

        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "StageChange",
            "Opportunity",
            id.ToString(),
            oldStage,
            newStage.ToString(),
            "Success",
            $"Stage changed to {newStage}",
            ipAddress);

        return new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            Stage = opp.Stage,
            Probability = opp.Probability,
            Status = opp.Status
        };
    }

    public async Task<bool> DeleteOpportunityAsync(
        int id,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var opp = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id, ct);
        if (opp == null) return false;

        _context.Opportunities.Remove(opp);
        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Delete",
            "Opportunity",
            id.ToString(),
            opp.OpportunityName,
            null,
            "Success",
            $"Opportunity deleted: {opp.OpportunityName}",
            ipAddress);

        return true;
    }

    public async Task<List<OpportunityDto>> GetAllOpportunitiesForPipelineAsync(
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var query = _context.Opportunities.Include(o => o.Customer).AsNoTracking().AsQueryable();

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(o => o.AssignedTo == currentUserId);
        }

        return await query
            .OrderByDescending(o => o.Amount)
            .Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedTo = o.AssignedTo
            })
            .ToListAsync(ct);
    }
}
