using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public CustomerService(IApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(
        string? searchTerm,
        CustomerStatus? status,
        string? currentUserId,
        string? currentUserRole,
        int pageIndex = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = _context.Customers
            .Include(c => c.Opportunities)
            .AsNoTracking()
            .AsQueryable();

        // Role-based scope filtering
        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(c => c.OwnerId == currentUserId);
        }

        // Status filter
        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        // Search filter: Name, Email, Phone, Company
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(searchTerm) ||
                c.Email.ToLower().Contains(searchTerm) ||
                c.Phone.Contains(searchTerm) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedDate)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                Notes = c.Notes,
                OwnerId = c.OwnerId,
                CreatedBy = c.CreatedBy,
                CreatedDate = c.CreatedDate,
                ModifiedDate = c.ModifiedDate,
                ActiveOpportunitiesCount = c.Opportunities.Count(o => o.Status == OpportunityStatus.Open),
                TotalOpportunityValue = (decimal)(c.Opportunities.Where(o => o.Status == OpportunityStatus.Open).Sum(o => (double?)o.Amount) ?? 0.0)
            })
            .ToListAsync(ct);

        return new PagedResult<CustomerDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(
        int id,
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var customer = await _context.Customers
            .Include(c => c.Opportunities)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == id, ct);

        if (customer == null) return null;

        // Authorization check for SalesExecutive
        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId) && customer.OwnerId != currentUserId)
        {
            throw new UnauthorizedAccessException("You are not authorized to access this customer record.");
        }

        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            Notes = customer.Notes,
            OwnerId = customer.OwnerId,
            CreatedBy = customer.CreatedBy,
            CreatedDate = customer.CreatedDate,
            ModifiedDate = customer.ModifiedDate,
            ActiveOpportunitiesCount = customer.Opportunities.Count(o => o.Status == OpportunityStatus.Open),
            TotalOpportunityValue = customer.Opportunities.Where(o => o.Status == OpportunityStatus.Open).Sum(o => (decimal?)o.Amount) ?? 0m
        };
    }

    public async Task<CustomerDto> CreateCustomerAsync(
        CreateCustomerDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        // Server-side validations & Duplicate prevention
        var cleanEmail = dto.Email.Trim().ToLower();
        var cleanPhone = dto.Phone.Trim();

        var emailExists = await _context.Customers.AnyAsync(c => c.Email.ToLower() == cleanEmail, ct);
        if (emailExists)
        {
            throw new BusinessRuleException($"A customer with email '{dto.Email}' already exists.");
        }

        var phoneExists = await _context.Customers.AnyAsync(c => c.Phone == cleanPhone, ct);
        if (phoneExists)
        {
            throw new BusinessRuleException($"A customer with phone number '{dto.Phone}' already exists.");
        }

        var customerCount = await _context.Customers.CountAsync(ct) + 1;
        var customerCode = $"CUST-{1000 + customerCount}";

        var customer = new Customer
        {
            CustomerCode = customerCode,
            CustomerName = dto.CustomerName.Trim(),
            Email = cleanEmail,
            Phone = cleanPhone,
            CompanyName = dto.CompanyName?.Trim(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            Status = dto.Status,
            Notes = dto.Notes?.Trim(),
            OwnerId = !string.IsNullOrEmpty(dto.OwnerId) ? dto.OwnerId : currentUserId,
            CreatedBy = currentUserId ?? "System",
            CreatedDate = DateTime.UtcNow
        };

        await _context.Customers.AddAsync(customer, ct);
        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Create",
            "Customer",
            customer.CustomerId.ToString(),
            null,
            $"{customer.CustomerName} ({customer.Email})",
            "Success",
            $"Customer created with Code {customer.CustomerCode}",
            ipAddress);

        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            Notes = customer.Notes,
            OwnerId = customer.OwnerId,
            CreatedBy = customer.CreatedBy,
            CreatedDate = customer.CreatedDate
        };
    }

    public async Task<CustomerDto> UpdateCustomerAsync(
        UpdateCustomerDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == dto.CustomerId, ct);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Customer with ID {dto.CustomerId} not found.");
        }

        var cleanEmail = dto.Email.Trim().ToLower();
        var cleanPhone = dto.Phone.Trim();

        var emailExists = await _context.Customers.AnyAsync(c => c.CustomerId != dto.CustomerId && c.Email.ToLower() == cleanEmail, ct);
        if (emailExists)
        {
            throw new BusinessRuleException($"Another customer with email '{dto.Email}' already exists.");
        }

        var phoneExists = await _context.Customers.AnyAsync(c => c.CustomerId != dto.CustomerId && c.Phone == cleanPhone, ct);
        if (phoneExists)
        {
            throw new BusinessRuleException($"Another customer with phone number '{dto.Phone}' already exists.");
        }

        var oldState = $"{customer.CustomerName}, {customer.Email}, {customer.Phone}, {customer.Status}";

        customer.CustomerName = dto.CustomerName.Trim();
        customer.Email = cleanEmail;
        customer.Phone = cleanPhone;
        customer.CompanyName = dto.CompanyName?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.City = dto.City?.Trim();
        customer.State = dto.State?.Trim();
        customer.Status = dto.Status;
        customer.Notes = dto.Notes?.Trim();
        if (!string.IsNullOrEmpty(dto.OwnerId))
        {
            customer.OwnerId = dto.OwnerId;
        }
        customer.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        var newState = $"{customer.CustomerName}, {customer.Email}, {customer.Phone}, {customer.Status}";

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Update",
            "Customer",
            customer.CustomerId.ToString(),
            oldState,
            newState,
            "Success",
            $"Customer updated: {customer.CustomerName}",
            ipAddress);

        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            Notes = customer.Notes,
            OwnerId = customer.OwnerId,
            ModifiedDate = customer.ModifiedDate
        };
    }

    public async Task<bool> DeactivateCustomerAsync(
        int id,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id, ct);
        if (customer == null) return false;

        customer.Status = CustomerStatus.Inactive;
        customer.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Deactivate",
            "Customer",
            id.ToString(),
            CustomerStatus.Active.ToString(),
            CustomerStatus.Inactive.ToString(),
            "Success",
            $"Customer deactivated: {customer.CustomerName}",
            ipAddress);

        return true;
    }

    public async Task<bool> DeleteCustomerAsync(
        int id,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id, ct);
        if (customer == null) return false;

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(ct);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Delete",
            "Customer",
            id.ToString(),
            customer.CustomerName,
            null,
            "Success",
            $"Customer deleted: {customer.CustomerName}",
            ipAddress);

        return true;
    }

    public async Task<List<CustomerDto>> GetAllActiveCustomersAsync(
        string? currentUserId,
        string? currentUserRole,
        CancellationToken ct = default)
    {
        var query = _context.Customers.AsNoTracking().Where(c => c.Status == CustomerStatus.Active);

        if (currentUserRole == "SalesExecutive" && !string.IsNullOrEmpty(currentUserId))
        {
            query = query.Where(c => c.OwnerId == currentUserId);
        }

        return await query
            .OrderBy(c => c.CustomerName)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName
            })
            .ToListAsync(ct);
    }
}
