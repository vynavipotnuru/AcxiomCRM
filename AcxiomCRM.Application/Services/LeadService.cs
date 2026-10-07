using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Application.Services;

public class LeadService : ILeadService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public LeadService(IApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<PagedResult<LeadDto>> GetLeadsAsync(
        string? searchTerm,
        LeadStatus? status,
        Priority? priority,
        string? currentUserId,
        string? currentUserRole,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = _context.Leads.AsNoTracking().AsQueryable();

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(l => l.AssignedTo == currentUserId);
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(l => l.Priority == priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();
            query = query.Where(l =>
                l.LeadName.ToLower().Contains(searchTerm) ||
                l.Email.ToLower().Contains(searchTerm) ||
                l.Phone.Contains(searchTerm) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(l => l.CreatedDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                AssignedTo = l.AssignedTo,
                Notes = l.Notes,
                CreatedDate = l.CreatedDate,
                ConvertedDate = l.ConvertedDate,
                ConvertedCustomerId = l.ConvertedCustomerId,
                ConvertedOpportunityId = l.ConvertedOpportunityId
            })
            .ToListAsync(ct);

        return new PagedResult<LeadDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<LeadDto?> GetLeadByIdAsync(
        int id,
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var lead = await _context.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.LeadId == id, ct);
        if (lead == null) return null;

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId) && lead.AssignedTo != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this lead.");
        }

        return new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            AssignedTo = lead.AssignedTo,
            Notes = lead.Notes,
            CreatedDate = lead.CreatedDate,
            ConvertedDate = lead.ConvertedDate,
            ConvertedCustomerId = lead.ConvertedCustomerId,
            ConvertedOpportunityId = lead.ConvertedOpportunityId
        };
    }

    public async Task<LeadDto> CreateLeadAsync(
        CreateLeadDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var cleanEmail = dto.Email.Trim().ToLower();
        var cleanPhone = dto.Phone.Trim();

        var count = await _context.Leads.CountAsync(ct) + 1;
        var leadCode = $"LEAD-{1000 + count}";

        var lead = new Lead
        {
            LeadCode = leadCode,
            LeadName = dto.LeadName.Trim(),
            Email = cleanEmail,
            Phone = cleanPhone,
            CompanyName = dto.CompanyName?.Trim(),
            Source = dto.Source?.Trim(),
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            AssignedTo = !string.IsNullOrEmpty(dto.AssignedTo) ? dto.AssignedTo : currentUserId,
            Notes = dto.Notes?.Trim(),
            CreatedDate = DateTime.UtcNow
        };

        await _context.Leads.AddAsync(lead, ct);
        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Create",
            "Lead",
            lead.LeadId.ToString(),
            null,
            $"{lead.LeadName} ({lead.Email})",
            "Success",
            $"Lead created with code {lead.LeadCode}",
            ipAddress);

        return new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            AssignedTo = lead.AssignedTo,
            Notes = lead.Notes,
            CreatedDate = lead.CreatedDate
        };
    }

    public async Task<LeadDto> UpdateLeadAsync(
        UpdateLeadDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == dto.LeadId, ct);
        if (lead == null)
        {
            throw new KeyNotFoundException($"Lead with ID {dto.LeadId} not found.");
        }

        // Validate status transition
        if (lead.Status != dto.Status)
        {
            LeadStatusTransitionRule.ValidateTransition(lead.Status, dto.Status);
        }

        var oldState = $"{lead.LeadName}, {lead.Status}, {lead.Priority}, {lead.ExpectedValue}";

        lead.LeadName = dto.LeadName.Trim();
        lead.Email = dto.Email.Trim().ToLower();
        lead.Phone = dto.Phone.Trim();
        lead.CompanyName = dto.CompanyName?.Trim();
        lead.Source = dto.Source?.Trim();
        lead.Status = dto.Status;
        lead.Priority = dto.Priority;
        lead.ExpectedValue = dto.ExpectedValue;
        if (!string.IsNullOrEmpty(dto.AssignedTo))
        {
            lead.AssignedTo = dto.AssignedTo;
        }
        lead.Notes = dto.Notes?.Trim();

        await _context.SaveChangesAsync(ct);

        var newState = $"{lead.LeadName}, {lead.Status}, {lead.Priority}, {lead.ExpectedValue}";

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Update",
            "Lead",
            lead.LeadId.ToString(),
            oldState,
            newState,
            "Success",
            $"Lead updated: {lead.LeadName}",
            ipAddress);

        return new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            AssignedTo = lead.AssignedTo,
            Notes = lead.Notes,
            CreatedDate = lead.CreatedDate
        };
    }

    public async Task<LeadDto> UpdateLeadStatusAsync(
        int leadId,
        LeadStatus newStatus,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId, ct);
        if (lead == null) throw new KeyNotFoundException($"Lead with ID {leadId} not found.");

        LeadStatusTransitionRule.ValidateTransition(lead.Status, newStatus);

        var oldStatus = lead.Status.ToString();
        lead.Status = newStatus;
        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "StatusChange",
            "Lead",
            leadId.ToString(),
            oldStatus,
            newStatus.ToString(),
            "Success",
            $"Lead status changed to {newStatus}",
            ipAddress);

        return new LeadDto
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Status = lead.Status
        };
    }

    public async Task<(CustomerDto Customer, OpportunityDto? Opportunity)> ConvertLeadAsync(
        ConvertLeadDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == dto.LeadId, ct);
        if (lead == null) throw new KeyNotFoundException($"Lead with ID {dto.LeadId} not found.");

        if (lead.Status == LeadStatus.Converted)
        {
            throw new BusinessRuleException("This lead has already been converted.");
        }

        // Validate or check existing customer with same email or phone
        var cleanEmail = dto.Email.Trim().ToLower();
        var cleanPhone = dto.Phone.Trim();

        var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == cleanEmail || c.Phone == cleanPhone, ct);
        Customer customer;

        if (existingCustomer != null)
        {
            customer = existingCustomer;
        }
        else
        {
            var customerCount = await _context.Customers.CountAsync(ct) + 1;
            customer = new Customer
            {
                CustomerCode = $"CUST-{1000 + customerCount}",
                CustomerName = dto.CustomerName.Trim(),
                Email = cleanEmail,
                Phone = cleanPhone,
                CompanyName = dto.CompanyName ?? lead.CompanyName,
                Status = CustomerStatus.Active,
                OwnerId = lead.AssignedTo ?? currentUserId,
                CreatedBy = currentUserId ?? "LeadConversion",
                CreatedDate = DateTime.UtcNow,
                Notes = $"Converted from Lead {lead.LeadCode}"
            };

            await _context.Customers.AddAsync(customer, ct);
            await _context.SaveChangesAsync(ct);
        }

        Opportunity? opportunity = null;
        if (dto.CreateOpportunity && dto.OpportunityAmount > 0)
        {
            var closeDate = dto.ExpectedCloseDate ?? DateTime.UtcNow.AddDays(30);
            OpportunityValidationRule.ValidateOpportunity(dto.OpportunityAmount, 50, closeDate, OpportunityStatus.Open);

            opportunity = new Opportunity
            {
                OpportunityName = !string.IsNullOrWhiteSpace(dto.OpportunityName) ? dto.OpportunityName.Trim() : $"Deal - {customer.CustomerName}",
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                Amount = dto.OpportunityAmount,
                Stage = OpportunityStage.Qualification,
                Probability = 50,
                ExpectedCloseDate = closeDate,
                Status = OpportunityStatus.Open,
                AssignedTo = lead.AssignedTo ?? currentUserId,
                Notes = $"Automatically generated from Lead conversion {lead.LeadCode}",
                CreatedDate = DateTime.UtcNow
            };

            await _context.Opportunities.AddAsync(opportunity, ct);
            await _context.SaveChangesAsync(ct);
        }

        // Mark lead as Converted
        lead.Status = LeadStatus.Converted;
        lead.ConvertedDate = DateTime.UtcNow;
        lead.ConvertedCustomerId = customer.CustomerId;
        lead.ConvertedOpportunityId = opportunity?.OpportunityId;

        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Convert",
            "Lead",
            lead.LeadId.ToString(),
            lead.LeadCode,
            $"Customer: {customer.CustomerCode}, Opp: {opportunity?.OpportunityId}",
            "Success",
            $"Lead converted into Customer {customer.CustomerName}",
            ipAddress);

        var custDto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Status = customer.Status
        };

        OpportunityDto? oppDto = null;
        if (opportunity != null)
        {
            oppDto = new OpportunityDto
            {
                OpportunityId = opportunity.OpportunityId,
                OpportunityName = opportunity.OpportunityName,
                CustomerId = opportunity.CustomerId,
                Amount = opportunity.Amount,
                Stage = opportunity.Stage,
                Probability = opportunity.Probability,
                ExpectedCloseDate = opportunity.ExpectedCloseDate,
                Status = opportunity.Status
            };
        }

        return (custDto, oppDto);
    }

    public async Task<bool> DeleteLeadAsync(
        int id,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == id, ct);
        if (lead == null) return false;

        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Delete",
            "Lead",
            id.ToString(),
            lead.LeadName,
            null,
            "Success",
            $"Lead deleted: {lead.LeadName}",
            ipAddress);

        return true;
    }

    public async Task<List<LeadDto>> GetAllActiveLeadsAsync(
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var query = _context.Leads.AsNoTracking().Where(l => l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost);

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(l => l.AssignedTo == currentUserId);
        }

        return await query
            .OrderBy(l => l.LeadName)
            .Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Status = l.Status
            })
            .ToListAsync(ct);
    }
}
