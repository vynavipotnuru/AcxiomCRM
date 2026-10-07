using System.Security.Claims;
using AcxiomCRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Customers()
    {
        var data = await _reportService.GetCustomerReportAsync(CurrentUserId, CurrentUserRole);
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> Leads()
    {
        var data = await _reportService.GetLeadReportAsync(CurrentUserId, CurrentUserRole);
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> Pipeline()
    {
        var data = await _reportService.GetPipelineReportAsync(CurrentUserId, CurrentUserRole);
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> Conversion()
    {
        var data = await _reportService.GetConversionReportAsync(CurrentUserId, CurrentUserRole);
        return View(data);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UserActivity()
    {
        var data = await _reportService.GetUserActivityReportAsync();
        return View(data);
    }
}
