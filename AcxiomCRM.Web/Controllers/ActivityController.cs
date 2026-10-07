using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ActivityController : Controller
{
    private readonly IActivityService _activityService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;

    public ActivityController(
        IActivityService activityService,
        ICustomerService customerService,
        ILeadService leadService)
    {
        _activityService = activityService;
        _customerService = customerService;
        _leadService = leadService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);
    private string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var activities = await _activityService.GetRecentActivitiesAsync(CurrentUserId, CurrentUserRole, 50);
        return View(activities);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
        var leads = await _leadService.GetAllActiveLeadsAsync(CurrentUserId, CurrentUserRole);

        ViewBag.Customers = customers;
        ViewBag.Leads = leads;

        return View(new CreateActivityDto
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.UtcNow
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateActivityDto model)
    {
        if (!ModelState.IsValid)
        {
            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var leads = await _leadService.GetAllActiveLeadsAsync(CurrentUserId, CurrentUserRole);
            ViewBag.Customers = customers;
            ViewBag.Leads = leads;
            return View(model);
        }

        var activity = await _activityService.CreateActivityAsync(model, CurrentUserId, RemoteIp);
        TempData["SuccessMessage"] = $"Activity '{activity.Subject}' recorded successfully.";

        if (model.CustomerId.HasValue)
        {
            return RedirectToAction("Details", "Customer", new { id = model.CustomerId.Value });
        }
        if (model.LeadId.HasValue)
        {
            return RedirectToAction("Details", "Lead", new { id = model.LeadId.Value });
        }

        return RedirectToAction(nameof(Index));
    }
}
