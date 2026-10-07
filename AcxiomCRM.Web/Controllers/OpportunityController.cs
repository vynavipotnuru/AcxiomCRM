using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class OpportunityController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly ICustomerService _customerService;
    private readonly IUserService _userService;

    public OpportunityController(
        IOpportunityService opportunityService,
        ICustomerService customerService,
        IUserService userService)
    {
        _opportunityService = opportunityService;
        _customerService = customerService;
        _userService = userService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);
    private string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, OpportunityStage? stage, OpportunityStatus? status, int pageIndex = 1)
    {
        var result = await _opportunityService.GetOpportunitiesAsync(searchTerm, stage, status, CurrentUserId, CurrentUserRole, pageIndex, 10);
        ViewBag.SearchTerm = searchTerm;
        ViewBag.Stage = stage;
        ViewBag.Status = status;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Pipeline()
    {
        var opportunities = await _opportunityService.GetAllOpportunitiesForPipelineAsync(CurrentUserId, CurrentUserRole);
        return View(opportunities);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var opp = await _opportunityService.GetOpportunityByIdAsync(id, CurrentUserId, CurrentUserRole);
            if (opp == null) return NotFound();

            return View(opp);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null)
    {
        var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
        var users = await _userService.GetAllUsersAsync();

        ViewBag.Customers = customers;
        ViewBag.Users = users;

        return View(new CreateOpportunityDto
        {
            CustomerId = customerId,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOpportunityDto model)
    {
        if (!ModelState.IsValid)
        {
            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Customers = customers;
            ViewBag.Users = users;
            return View(model);
        }

        try
        {
            var opp = await _opportunityService.CreateOpportunityAsync(model, CurrentUserId, RemoteIp);
            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Customers = customers;
            ViewBag.Users = users;
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var opp = await _opportunityService.GetOpportunityByIdAsync(id, CurrentUserId, CurrentUserRole);
            if (opp == null) return NotFound();

            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var users = await _userService.GetAllUsersAsync();

            ViewBag.Customers = customers;
            ViewBag.Users = users;

            var dto = new UpdateOpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedTo = opp.AssignedTo,
                Notes = opp.Notes
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
    public async Task<IActionResult> Edit(UpdateOpportunityDto model)
    {
        if (!ModelState.IsValid)
        {
            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Customers = customers;
            ViewBag.Users = users;
            return View(model);
        }

        try
        {
            await _opportunityService.UpdateOpportunityAsync(model, CurrentUserId, RemoteIp);
            TempData["SuccessMessage"] = $"Opportunity '{model.OpportunityName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var customers = await _customerService.GetAllActiveCustomersAsync(CurrentUserId, CurrentUserRole);
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Customers = customers;
            ViewBag.Users = users;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStage(int id, OpportunityStage newStage)
    {
        try
        {
            await _opportunityService.UpdateOpportunityStageAsync(id, newStage, CurrentUserId, RemoteIp);
            TempData["SuccessMessage"] = $"Opportunity stage updated to {newStage}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _opportunityService.DeleteOpportunityAsync(id, CurrentUserId, RemoteIp);
        if (success)
        {
            TempData["SuccessMessage"] = "Opportunity deleted successfully.";
        }
        return RedirectToAction(nameof(Index));
    }
}
