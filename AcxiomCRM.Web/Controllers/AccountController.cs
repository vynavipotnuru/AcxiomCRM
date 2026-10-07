using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLog;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLog)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditLog = auditLog;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginDto());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            await _auditLog.LogAsync(
                null,
                model.Email,
                "FailedLogin",
                "Authentication",
                null,
                null,
                null,
                "Failure",
                $"Invalid login attempt for non-existent email '{model.Email}'",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an Administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _auditLog.LogAsync(
                user.Id,
                user.Email,
                "Login",
                "Authentication",
                user.Id,
                null,
                null,
                "Success",
                "User successfully logged in.",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _auditLog.LogAsync(
                user.Id,
                user.Email,
                "FailedLogin",
                "Authentication",
                user.Id,
                null,
                null,
                "LockedOut",
                "Account locked out due to repeated failed login attempts.",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            ModelState.AddModelError(string.Empty, "This account has been temporarily locked out due to multiple failed login attempts. Please try again later.");
            return View(model);
        }

        await _auditLog.LogAsync(
            user.Id,
            user.Email,
            "FailedLogin",
            "Authentication",
            user.Id,
            null,
            null,
            "Failure",
            "Invalid password provided.",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        ModelState.AddModelError(string.Empty, "Invalid login credentials.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name;
        var userId = _userManager.GetUserId(User);

        await _signInManager.SignOutAsync();

        await _auditLog.LogAsync(
            userId,
            userName,
            "Logout",
            "Authentication",
            userId,
            null,
            null,
            "Success",
            "User logged out.",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordDto());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);

            await _auditLog.LogAsync(
                user.Id,
                user.Email,
                "Security",
                "User",
                user.Id,
                null,
                null,
                "Success",
                "User successfully changed their password.",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = "Your password has been updated successfully.";
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordDto());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        // Do not disclose whether email exists or not
        TempData["InfoMessage"] = "If an account exists for that email, password reset instructions have been sent.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
