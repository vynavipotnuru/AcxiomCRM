using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;

    public OpportunitiesApiController(IOpportunityService opportunityService)
    {
        _opportunityService = opportunityService;
    }

    private string? CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User?.FindFirstValue(ClaimTypes.Role);
    private string? RemoteIp => HttpContext?.Connection?.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> GetOpportunities([FromQuery] string? search, [FromQuery] OpportunityStage? stage, [FromQuery] OpportunityStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _opportunityService.GetOpportunitiesAsync(search, stage, status, CurrentUserId, CurrentUserRole, page, pageSize);
        return Ok(ApiResponse<PagedResult<OpportunityDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<string>.Fail("Validation failed", errors));
        }

        try
        {
            var created = await _opportunityService.CreateOpportunityAsync(dto, CurrentUserId, RemoteIp);
            return StatusCode(201, ApiResponse<OpportunityDto>.Ok(created, "Opportunity created successfully."));
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }
}
