using Ekomart.Infrastructure.Identity;
using Ekomart.Web.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(await BuildModelAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            model.Email = user.Email ?? string.Empty;
            model.Roles = (await _userManager.GetRolesAsync(user)).ToArray();
            return View(model);
        }

        user.FullName = model.FullName;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Email = user.Email ?? string.Empty;
            model.Roles = (await _userManager.GetRolesAsync(user)).ToArray();
            return View(model);
        }

        TempData["ProfileSaved"] = "Данные профиля сохранены.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileViewModel> BuildModelAsync(ApplicationUser user)
    {
        return new ProfileViewModel
        {
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? user.UserName ?? string.Empty,
            Roles = (await _userManager.GetRolesAsync(user)).ToArray()
        };
    }
}
