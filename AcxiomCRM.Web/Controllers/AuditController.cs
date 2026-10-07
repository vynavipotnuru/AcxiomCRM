using AcxiomCRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class AuditController : Controller
{
    private readonly IAuditLogService _auditLogService;

    public AuditController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm,
        string? entityName,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        int pageIndex = 1)
    {
        var result = await _auditLogService.GetAuditLogsAsync(searchTerm, entityName, action, fromDate, toDate, pageIndex, 15);
        ViewBag.SearchTerm = searchTerm;
        ViewBag.EntityName = entityName;
        ViewBag.Action = action;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        return View(result);
    }
}
