using Ekomart.Application.Interfaces;
using Ekomart.Domain.Enums;
using Ekomart.Infrastructure.Identity;
using Ekomart.Web.Authorization;
using Ekomart.Web.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Ekomart.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuditService _auditService;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService,
        IStringLocalizer<SharedResource> localizer)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _auditService = auditService;
        _localizer = localizer;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            await LogAuditAsync(
                AuditAction.Login,
                nameof(ApplicationUser),
                user?.Id,
                user?.Id,
                $"Successful login for {model.Email}.");

            return RedirectToLocal(model.ReturnUrl);
        }

        await LogAuditAsync(
            AuditAction.Login,
            nameof(ApplicationUser),
            details: $"Failed login attempt for {model.Email}.");

        ModelState.AddModelError(string.Empty, _localizer["Invalid email or password."].Value);
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        if (!await EnsureUserRoleAsync())
        {
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.User);
        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        await LogAuditAsync(
            AuditAction.Register,
            nameof(ApplicationUser),
            user.Id,
            user.Id,
            $"Registered user {user.Email}.");

        return RedirectToLocal(model.ReturnUrl);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await LogAuditAsync(
            AuditAction.Logout,
            nameof(ApplicationUser),
            userId,
            userId,
            "User signed out.");

        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return RedirectToAction("Forbidden", "Errors");
    }

    private async Task<bool> EnsureUserRoleAsync()
    {
        if (!await _roleManager.RoleExistsAsync(AppRoles.User))
        {
            var result = await _roleManager.CreateAsync(new IdentityRole(AppRoles.User));
            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return false;
            }
        }

        return true;
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private Task LogAuditAsync(
        AuditAction action,
        string entityName,
        string? entityId = null,
        string? userId = null,
        string? details = null)
    {
        return _auditService.LogAsync(
            action,
            entityName,
            entityId,
            userId,
            details,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers["User-Agent"].ToString(),
            HttpContext.RequestAborted);
    }
}
