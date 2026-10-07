using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/reports")]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsApiController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private string? CurrentUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User?.FindFirstValue(ClaimTypes.Role);

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipelineReport()
    {
        var data = await _reportService.GetPipelineReportAsync(CurrentUserId, CurrentUserRole);
        return Ok(ApiResponse<List<PipelineReportDto>>.Ok(data));
    }
}
