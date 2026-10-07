using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class LeadController : Controller
{
    private readonly ILeadService _leadService;
    private readonly IActivityService _activityService;
    private readonly IUserService _userService;

    public LeadController(
        ILeadService leadService,
        IActivityService activityService,
        IUserService userService)
    {
        _leadService = leadService;
        _activityService = activityService;
        _userService = userService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);
    private string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, LeadStatus? status, Priority? priority, int pageIndex = 1)
    {
        var result = await _leadService.GetLeadsAsync(searchTerm, status, priority, CurrentUserId, CurrentUserRole, pageIndex, 10);
        ViewBag.SearchTerm = searchTerm;
        ViewBag.Status = status;
        ViewBag.Priority = priority;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var lead = await _leadService.GetLeadByIdAsync(id, CurrentUserId, CurrentUserRole);
            if (lead == null) return NotFound();

            var activities = await _activityService.GetActivitiesForLeadAsync(id);
            ViewBag.Activities = activities;

            return View(lead);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var users = await _userService.GetAllUsersAsync();
        ViewBag.Users = users;
        return View(new CreateLeadDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLeadDto model)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users;
            return View(model);
        }

        try
        {
            var lead = await _leadService.CreateLeadAsync(model, CurrentUserId, RemoteIp);
            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' ({lead.LeadCode}) created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var lead = await _leadService.GetLeadByIdAsync(id, CurrentUserId, CurrentUserRole);
            if (lead == null) return NotFound();

            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users;

            var dto = new UpdateLeadDto
            {
                LeadId = lead.LeadId,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                AssignedTo = lead.AssignedTo,
                Notes = lead.Notes
            };

            return View(dto);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateLeadDto model)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users;
            return View(model);
        }

        try
        {
            await _leadService.UpdateLeadAsync(model, CurrentUserId, RemoteIp);
            TempData["SuccessMessage"] = $"Lead '{model.LeadName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (lead == null) return NotFound();

        if (lead.Status == LeadStatus.Converted)
        {
            TempData["ErrorMessage"] = "Lead is already converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var dto = new ConvertLeadDto
        {
            LeadId = lead.LeadId,
            CustomerName = lead.LeadName,
            CompanyName = lead.CompanyName,
            Email = lead.Email,
            Phone = lead.Phone,
            CreateOpportunity = true,
            OpportunityName = $"Deal - {lead.CompanyName ?? lead.LeadName}",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 100000m,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(ConvertLeadDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var (customer, opportunity) = await _leadService.ConvertLeadAsync(model, CurrentUserId, RemoteIp);
            TempData["SuccessMessage"] = $"Lead converted successfully to Customer '{customer.CustomerName}'" +
                                         (opportunity != null ? $" and Opportunity '{opportunity.OpportunityName}'." : ".");
            return RedirectToAction("Details", "Customer", new { id = customer.CustomerId });
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _leadService.DeleteLeadAsync(id, CurrentUserId, RemoteIp);
        if (success)
        {
            TempData["SuccessMessage"] = "Lead deleted successfully.";
        }
        return RedirectToAction(nameof(Index));
    }
}
