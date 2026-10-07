using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/leads")]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsApiController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    private string? CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User?.FindFirstValue(ClaimTypes.Role);
    private string? RemoteIp => HttpContext?.Connection?.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> GetLeads([FromQuery] string? search, [FromQuery] LeadStatus? status, [FromQuery] Priority? priority, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _leadService.GetLeadsAsync(search, status, priority, CurrentUserId, CurrentUserRole, page, pageSize);
        return Ok(ApiResponse<PagedResult<LeadDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLeadById(int id)
    {
        try
        {
            var lead = await _leadService.GetLeadByIdAsync(id, CurrentUserId, CurrentUserRole);
            if (lead == null)
            {
                return NotFound(ApiResponse<string>.Fail($"Lead with ID {id} was not found."));
            }
            return Ok(ApiResponse<LeadDto>.Ok(lead));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<string>.Fail("Validation failed", errors));
        }

        try
        {
            var created = await _leadService.CreateLeadAsync(dto, CurrentUserId, RemoteIp);
            return StatusCode(201, ApiResponse<LeadDto>.Ok(created, "Lead created successfully."));
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }
}
