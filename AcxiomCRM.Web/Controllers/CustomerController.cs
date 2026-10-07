using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.BusinessRules;
using AcxiomCRM.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly IActivityService _activityService;
    private readonly IUserService _userService;

    public CustomerController(
        ICustomerService customerService,
        IActivityService activityService,
        IUserService userService)
    {
        _customerService = customerService;
        _activityService = activityService;
        _userService = userService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);
    private string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, CustomerStatus? status, int pageIndex = 1)
    {
        var result = await _customerService.GetCustomersAsync(searchTerm, status, CurrentUserId, CurrentUserRole, pageIndex, 10);
        ViewBag.SearchTerm = searchTerm;
        ViewBag.Status = status;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id, CurrentUserId, CurrentUserRole);
            if (customer == null) return NotFound();

            var activities = await _activityService.GetActivitiesForCustomerAsync(id);
            ViewBag.Activities = activities;

            return View(customer);
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
        return View(new CreateCustomerDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCustomerDto model)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users;
            return View(model);
        }

        try
        {
            var customer = await _customerService.CreateCustomerAsync(model, CurrentUserId, RemoteIp);
            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' ({customer.CustomerCode}) created successfully.";
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
            var customer = await _customerService.GetCustomerByIdAsync(id, CurrentUserId, CurrentUserRole);
            if (customer == null) return NotFound();

            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users;

            var dto = new UpdateCustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                Notes = customer.Notes,
                OwnerId = customer.OwnerId
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
    public async Task<IActionResult> Edit(UpdateCustomerDto model)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userService.GetAllUsersAsync();
            ViewBag.Users = users;
            return View(model);
        }

        try
        {
            await _customerService.UpdateCustomerAsync(model, CurrentUserId, RemoteIp);
            TempData["SuccessMessage"] = $"Customer '{model.CustomerName}' updated successfully.";
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var success = await _customerService.DeactivateCustomerAsync(id, CurrentUserId, RemoteIp);
        if (success)
        {
            TempData["SuccessMessage"] = "Customer has been deactivated.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _customerService.DeleteCustomerAsync(id, CurrentUserId, RemoteIp);
        if (success)
        {
            TempData["SuccessMessage"] = "Customer deleted successfully.";
        }
        return RedirectToAction(nameof(Index));
    }
}
