using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class FollowUpController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;

    public FollowUpController(
        IFollowUpService followUpService,
        ICustomerService customerService,
        ILeadService leadService,
        IUserService userService)
    {
        _followUpService = followUpService;
        _customerService = customerService;
        _leadService = leadService;
        _userService = userService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);
    private string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, FollowUpStatus? status, FollowUpType? type, int pageIndex = 1)
    {
        var result = await _followUpService.GetFollowUpsAsync(searchTerm, status, type, CurrentUserId, CurrentUserRole, pageIndex, 10);
        ViewBag.SearchTerm = searchTerm;
        ViewBag.Status = status;
        ViewBag.Type = type;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
        var leads = await _leadService.GetAllActiveLeadsAsync(CurrentUserId, CurrentUserRole);
        var users = await _userService.GetAllUsersAsync();

        ViewBag.Customers = customers;
        ViewBag.Leads = leads;
        ViewBag.Users = users;

        return View(new CreateFollowUpDto
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.UtcNow.AddDays(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFollowUpDto model)
    {
        if (!ModelState.IsValid)
        {
            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var leads = await _leadService.GetAllActiveLeadsAsync(CurrentUserId, CurrentUserRole);
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Customers = customers;
            ViewBag.Leads = leads;
            ViewBag.Users = users;
            return View(model);
        }

        try
        {
            var followUp = await _followUpService.CreateFollowUpAsync(model, CurrentUserId, RemoteIp);
            TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' scheduled successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var leads = await _leadService.GetAllActiveLeadsAsync(CurrentUserId, CurrentUserRole);
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Customers = customers;
            ViewBag.Leads = leads;
            ViewBag.Users = users;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var followUp = await _followUpService.GetFollowUpByIdAsync(id, CurrentUserId, CurrentUserRole);
            if (followUp == null) return NotFound();

            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var leads = await _leadService.GetAllActiveLeadsAsync(CurrentUserId, CurrentUserRole);
            var users = await _userService.GetAllUsersAsync();

            ViewBag.Customers = customers;
            ViewBag.Leads = leads;
            ViewBag.Users = users;

            var dto = new UpdateFollowUpDto
            {
                FollowUpId = followUp.FollowUpId,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                OpportunityId = followUp.OpportunityId,
                FollowUpDate = followUp.FollowUpDate,
                FollowUpType = followUp.FollowUpType,
                Subject = followUp.Subject,
                Remarks = followUp.Remarks,
                Status = followUp.Status,
                AssignedTo = followUp.AssignedTo
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
    public async Task<IActionResult> Edit(UpdateFollowUpDto model)
    {
        if (!ModelState.IsValid)
        {
            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var leads = await _leadService.GetAllActiveLeadsAsync(CurrentUserId, CurrentUserRole);
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Customers = customers;
            ViewBag.Leads = leads;
            ViewBag.Users = users;
            return View(model);
        }

        try
        {
            await _followUpService.UpdateFollowUpAsync(model, CurrentUserId, RemoteIp);
            TempData["SuccessMessage"] = $"Follow-up '{model.Subject}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var leads = await _leadService.GetAllActiveLeadsAsync(CurrentUserId, CurrentUserRole);
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Customers = customers;
            ViewBag.Leads = leads;
            ViewBag.Users = users;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks)
    {
        var success = await _followUpService.MarkCompleteAsync(id, remarks, CurrentUserId, RemoteIp);
        if (success)
        {
            TempData["SuccessMessage"] = "Follow-up marked as completed.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? remarks)
    {
        try
        {
            var success = await _followUpService.RescheduleAsync(id, newDate, remarks, CurrentUserId, RemoteIp);
            if (success)
            {
                TempData["SuccessMessage"] = "Follow-up rescheduled successfully.";
            }
        }
        catch (BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? remarks)
    {
        var success = await _followUpService.CancelAsync(id, remarks, CurrentUserId, RemoteIp);
        if (success)
        {
            TempData["SuccessMessage"] = "Follow-up marked as cancelled.";
        }
        return RedirectToAction(nameof(Index));
    }
}
