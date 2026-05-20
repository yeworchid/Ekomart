using Ekomart.Web.Authorization;
using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Users;
using Ekomart.Application.Interfaces;
using Ekomart.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class UsersController : Controller
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<IActionResult> Index(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return View(new AdminUsersIndexViewModel
        {
            Request = request,
            Users = await _userManagementService.GetUsersAsync(request, cancellationToken),
            Roles = await _userManagementService.GetRolesAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRoles(
        string userId,
        string[] roles,
        CancellationToken cancellationToken)
    {
        try
        {
            await _userManagementService.UpdateRolesAsync(
                new UserRoleUpdateDto
                {
                    UserId = userId,
                    Roles = roles
                },
                CurrentUserId(),
                cancellationToken);

            TempData["Success"] = "User roles updated.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private string CurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
