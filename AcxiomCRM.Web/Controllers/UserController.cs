using System.Security.Claims;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userService.GetAllUsersAsync();
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var roles = await _userService.GetAllRolesAsync();
        ViewBag.Roles = roles;
        return View(new CreateUserDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserDto model)
    {
        if (!ModelState.IsValid)
        {
            var roles = await _userService.GetAllRolesAsync();
            ViewBag.Roles = roles;
            return View(model);
        }

        var (success, error) = await _userService.CreateUserAsync(model, CurrentUserId, RemoteIp);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create user.");
            var roles = await _userService.GetAllRolesAsync();
            ViewBag.Roles = roles;
            return View(model);
        }

        TempData["SuccessMessage"] = $"User '{model.FullName}' ({model.Email}) created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userService.GetAllRolesAsync();
        ViewBag.Roles = roles;

        var dto = new EditUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsActive = user.IsActive
        };

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserDto model)
    {
        if (!ModelState.IsValid)
        {
            var roles = await _userService.GetAllRolesAsync();
            ViewBag.Roles = roles;
            return View(model);
        }

        var (success, error) = await _userService.UpdateUserAsync(model, CurrentUserId, RemoteIp);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update user.");
            var roles = await _userService.GetAllRolesAsync();
            ViewBag.Roles = roles;
            return View(model);
        }

        TempData["SuccessMessage"] = $"User '{model.FullName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var success = await _userService.ToggleUserStatusAsync(id, CurrentUserId, RemoteIp);
        if (success)
        {
            TempData["SuccessMessage"] = "User status updated.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            TempData["ErrorMessage"] = "Password must be at least 8 characters long.";
            return RedirectToAction(nameof(Index));
        }

        var success = await _userService.ResetPasswordAsync(id, newPassword, CurrentUserId, RemoteIp);
        if (success)
        {
            TempData["SuccessMessage"] = "Password reset successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to reset password.";
        }

        return RedirectToAction(nameof(Index));
    }
}
