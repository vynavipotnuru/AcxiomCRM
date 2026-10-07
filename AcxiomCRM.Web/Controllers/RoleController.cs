using AcxiomCRM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = "Admin")]
public class RoleController : Controller
{
    private readonly IUserService _userService;

    public RoleController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var roles = await _userService.GetAllRolesAsync();
        return View(roles);
    }
}
