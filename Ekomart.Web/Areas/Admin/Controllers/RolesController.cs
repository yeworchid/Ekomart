using Ekomart.Web.Authorization;
using Ekomart.Application.Interfaces;
using Ekomart.Infrastructure.Identity;
using Ekomart.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class RolesController : Controller
{
    private readonly IUserManagementService _userManagementService;
    private readonly UserManager<ApplicationUser> _userManager;

    public RolesController(
        IUserManagementService userManagementService,
        UserManager<ApplicationUser> userManager)
    {
        _userManagementService = userManagementService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var roleNames = await _userManagementService.GetRolesAsync(cancellationToken);
        var roles = new List<AdminRoleSummaryViewModel>(roleNames.Count);

        foreach (var roleName in roleNames)
        {
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            roles.Add(new AdminRoleSummaryViewModel
            {
                Name = roleName,
                UsersCount = users.Count
            });
        }

        return View(new AdminRolesIndexViewModel
        {
            Roles = roles
        });
    }
}
