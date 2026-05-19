using Ekomart.Application.DTOs.Orders;
using Ekomart.Application.Interfaces;
using Ekomart.Infrastructure.Identity;
using Ekomart.Web.Models.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(
        ICartService cartService,
        IOrderService orderService,
        ISubscriptionService subscriptionService,
        UserManager<ApplicationUser> userManager)
    {
        _cartService = cartService;
        _orderService = orderService;
        _subscriptionService = subscriptionService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var checkout = new CheckoutDto
        {
            CustomerName = user.FullName ?? user.UserName ?? string.Empty,
            CustomerEmail = user.Email ?? string.Empty,
            CustomerPhone = user.PhoneNumber ?? string.Empty
        };

        return View(await BuildCheckoutModelAsync(user.Id, checkout, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(
        CheckoutDto checkout,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (!ModelState.IsValid)
        {
            return View(await BuildCheckoutModelAsync(userId, checkout, cancellationToken));
        }

        try
        {
            var order = await _orderService.CheckoutAsync(userId, checkout, cancellationToken);
            TempData["CheckoutSuccess"] = $"Заказ #{order.Id} оформлен. Итог: {StoreViewHelpers.Money(order.TotalAmount)}.";
            return RedirectToAction("Index", "Profile");
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(await BuildCheckoutModelAsync(userId, checkout, cancellationToken));
        }
    }

    private async Task<CheckoutPageViewModel> BuildCheckoutModelAsync(
        string userId,
        CheckoutDto checkout,
        CancellationToken cancellationToken)
    {
        return new CheckoutPageViewModel
        {
            Checkout = checkout,
            Cart = await _cartService.GetCartAsync(userId, cancellationToken),
            CurrentSubscription = await _subscriptionService.GetCurrentUserSubscriptionAsync(userId, cancellationToken),
            Plans = await _subscriptionService.GetActivePlansAsync(cancellationToken)
        };
    }

    private string GetUserId()
    {
        return _userManager.GetUserId(User)
            ?? throw new InvalidOperationException("Authenticated user id was not found.");
    }
}
