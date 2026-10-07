using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/customers")]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersApiController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    private string? CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User?.FindFirstValue(ClaimTypes.Role);
    private string? RemoteIp => HttpContext?.Connection?.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] string? search, [FromQuery] CustomerStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _customerService.GetCustomersAsync(search, status, CurrentUserId, CurrentUserRole, page, pageSize);
        return Ok(ApiResponse<PagedResult<CustomerDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id, CurrentUserId, CurrentUserRole);
            if (customer == null)
            {
                return NotFound(ApiResponse<string>.Fail($"Customer with ID {id} was not found."));
            }
            return Ok(ApiResponse<CustomerDto>.Ok(customer));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<string>.Fail("Validation failed", errors));
        }

        try
        {
            var created = await _customerService.CreateCustomerAsync(dto, CurrentUserId, RemoteIp);
            return StatusCode(201, ApiResponse<CustomerDto>.Ok(created, "Customer created successfully."));
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ApiResponse<string>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (id != dto.CustomerId)
        {
            return BadRequest(ApiResponse<string>.Fail("Route ID does not match entity ID."));
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<string>.Fail("Validation failed", errors));
        }

        try
        {
            var updated = await _customerService.UpdateCustomerAsync(dto, CurrentUserId, RemoteIp);
            return Ok(ApiResponse<CustomerDto>.Ok(updated, "Customer updated successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(ApiResponse<string>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var deleted = await _customerService.DeleteCustomerAsync(id, CurrentUserId, RemoteIp);
        if (!deleted)
        {
            return NotFound(ApiResponse<string>.Fail($"Customer with ID {id} not found."));
        }
        return Ok(ApiResponse<string>.Ok("Customer deleted successfully."));
    }
}
