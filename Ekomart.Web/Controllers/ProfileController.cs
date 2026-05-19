using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Subscriptions;
using Ekomart.Application.Interfaces;
using Ekomart.Infrastructure.Identity;
using Ekomart.Web.Models.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IOrderService orderService,
        ISubscriptionService subscriptionService)
    {
        _userManager = userManager;
        _orderService = orderService;
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(await BuildModelAsync(user, pageNumber, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        ProfileViewModel model,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await PopulateProfileModelAsync(model, user, 1, cancellationToken);
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

            await PopulateProfileModelAsync(model, user, 1, cancellationToken);
            return View(model);
        }

        TempData["ProfileSaved"] = "Данные профиля сохранены.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenewSubscription(
        RenewSubscriptionDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["SubscriptionError"] = "Выберите тариф подписки.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var subscription = await _subscriptionService.RenewAsync(
                GetUserId(),
                dto,
                cancellationToken);

            TempData["SubscriptionMessage"] = $"Подписка {subscription.PlanName} активна до {subscription.EndsAtUtc:d}.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["SubscriptionError"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileViewModel> BuildModelAsync(
        ApplicationUser user,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        var model = new ProfileViewModel
        {
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? user.UserName ?? string.Empty
        };

        await PopulateProfileModelAsync(model, user, pageNumber, cancellationToken);
        return model;
    }

    private async Task PopulateProfileModelAsync(
        ProfileViewModel model,
        ApplicationUser user,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        model.Email = user.Email ?? string.Empty;
        model.Roles = (await _userManager.GetRolesAsync(user)).ToArray();
        model.Orders = await _orderService.GetUserOrdersAsync(
            user.Id,
            new PagedRequest { PageNumber = pageNumber, PageSize = 5 },
            cancellationToken);
        model.CurrentSubscription = await _subscriptionService.GetCurrentUserSubscriptionAsync(user.Id, cancellationToken);
        model.SubscriptionPlans = await _subscriptionService.GetActivePlansAsync(cancellationToken);
    }

    private string GetUserId()
    {
        return _userManager.GetUserId(User)
            ?? throw new InvalidOperationException("Authenticated user id was not found.");
    }
}
