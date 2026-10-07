using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLog;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLog)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditLog = auditLog;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.Fail("Invalid login payload."));
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            await _auditLog.LogAsync(
                null,
                model.Email,
                "ApiFailedLogin",
                "Authentication",
                null,
                null,
                null,
                "Failure",
                "API login attempt failed for inactive or non-existent user",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Unauthorized(ApiResponse<string>.Fail("Invalid email or password."));
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);

            await _auditLog.LogAsync(
                user.Id,
                user.Email,
                "ApiLogin",
                "Authentication",
                user.Id,
                null,
                null,
                "Success",
                "User logged in via REST API",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(ApiResponse<object>.Ok(new
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles
            }, "Authentication successful."));
        }

        if (result.IsLockedOut)
        {
            return StatusCode(423, ApiResponse<string>.Fail("Account is locked out due to multiple failed login attempts."));
        }

        return Unauthorized(ApiResponse<string>.Fail("Invalid email or password."));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(ApiResponse<string>.Ok("Logged out successfully."));
    }
}
