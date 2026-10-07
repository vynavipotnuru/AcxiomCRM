using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/followups")]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;

    public FollowUpsApiController(IFollowUpService followUpService)
    {
        _followUpService = followUpService;
    }

    private string? CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User?.FindFirstValue(ClaimTypes.Role);
    private string? RemoteIp => HttpContext?.Connection?.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> GetFollowUps([FromQuery] string? search, [FromQuery] FollowUpStatus? status, [FromQuery] FollowUpType? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _followUpService.GetFollowUpsAsync(search, status, type, CurrentUserId, CurrentUserRole, page, pageSize);
        return Ok(ApiResponse<PagedResult<FollowUpDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<string>.Fail("Validation failed", errors));
        }

        try
        {
            var created = await _followUpService.CreateFollowUpAsync(dto, CurrentUserId, RemoteIp);
            return StatusCode(201, ApiResponse<FollowUpDto>.Ok(created, "Follow-up created successfully."));
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }
}
