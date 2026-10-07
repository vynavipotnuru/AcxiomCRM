using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Infrastructure.Identity;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IAuditLogService _auditLog;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IAuditLogService auditLog)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditLog = auditLog;
    }

    public async Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync(ct);
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            result.Add(new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? "SalesExecutive",
                IsActive = user.IsActive,
                IsLockedOut = isLockedOut,
                CreatedDate = user.CreatedDate
            });
        }

        return result;
    }

    public async Task<UserDto?> GetUserByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var isLockedOut = await _userManager.IsLockedOutAsync(user);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Role = roles.FirstOrDefault() ?? "SalesExecutive",
            IsActive = user.IsActive,
            IsLockedOut = isLockedOut,
            CreatedDate = user.CreatedDate
        };
    }

    public async Task<(bool Success, string? Error)> CreateUserAsync(
        CreateUserDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing != null)
        {
            return (false, $"A user with email '{dto.Email}' already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            IsActive = true,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, errors);
        }

        if (!await _roleManager.RoleExistsAsync(dto.Role))
        {
            await _roleManager.CreateAsync(new ApplicationRole(dto.Role));
        }

        await _userManager.AddToRoleAsync(user, dto.Role);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Create",
            "User",
            user.Id,
            null,
            $"{user.FullName} ({user.Email}) - Role: {dto.Role}",
            "Success",
            $"User account created with role {dto.Role}",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateUserAsync(
        EditUserDto dto,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(dto.Id);
        if (user == null) return (false, "User not found.");

        var oldState = $"{user.FullName}, {user.Email}, Active: {user.IsActive}";

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim();
        user.IsActive = dto.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(dto.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);

            await _auditLog.LogAsync(
                currentUserId,
                currentUserId,
                "RoleChange",
                "User",
                user.Id,
                string.Join(", ", currentRoles),
                dto.Role,
                "Success",
                $"User role updated to {dto.Role}",
                ipAddress);
        }

        var newState = $"{user.FullName}, {user.Email}, Active: {user.IsActive}";

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "Update",
            "User",
            user.Id,
            oldState,
            newState,
            "Success",
            $"User account updated",
            ipAddress);

        return (true, null);
    }

    public async Task<bool> ToggleUserStatusAsync(
        string id,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAsync(
            currentUserId,
            currentUserId,
            "StatusChange",
            "User",
            user.Id,
            (!user.IsActive).ToString(),
            user.IsActive.ToString(),
            "Success",
            $"User active status set to {user.IsActive}",
            ipAddress);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(
        string userId,
        string newPassword,
        string? currentUserId,
        string? ipAddress,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

        if (result.Succeeded)
        {
            await _auditLog.LogAsync(
                currentUserId,
                currentUserId,
                "Security",
                "User",
                user.Id,
                null,
                null,
                "Success",
                $"Admin reset password for user {user.Email}",
                ipAddress);
            return true;
        }

        return false;
    }

    public async Task<List<RoleDto>> GetAllRolesAsync(CancellationToken ct = default)
    {
        return await _roleManager.Roles
            .AsNoTracking()
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                Description = r.Description
            })
            .ToListAsync(ct);
    }
}
