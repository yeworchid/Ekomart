using Ekomart.Application.DTOs.Orders;
using Ekomart.Application.Interfaces;
using Ekomart.Domain.Enums;
using Ekomart.Infrastructure.Identity;
using Ekomart.Web.Hubs;
using Ekomart.Web.Models.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Ekomart.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IHubContext<OrderHub> _orderHub;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(
        ICartService cartService,
        IOrderService orderService,
        ISubscriptionService subscriptionService,
        IHubContext<OrderHub> orderHub,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer)
    {
        _cartService = cartService;
        _orderService = orderService;
        _subscriptionService = subscriptionService;
        _orderHub = orderHub;
        _userManager = userManager;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(
        DeliveryMethod? deliveryMethod,
        CancellationToken cancellationToken)
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
            CustomerPhone = user.PhoneNumber ?? string.Empty,
            DeliveryMethod = deliveryMethod
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
            await _orderHub.Clients.Group(OrderHub.ManagersGroup).SendAsync(
                "OrderCreated",
                BuildOrderCreatedPayload(order),
                cancellationToken);

            TempData["CheckoutSuccess"] = _localizer[
                "Order #{0} has been placed. Total: {1}.",
                order.Id,
                StoreViewHelpers.Money(order.TotalAmount)].Value;
            return RedirectToAction("Index", "Profile");
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, _localizer[exception.Message].Value);
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

    private object BuildOrderCreatedPayload(OrderDto order)
    {
        var customer = string.IsNullOrWhiteSpace(order.CustomerName)
            ? _localizer["Customer"].Value
            : order.CustomerName;

        return new
        {
            id = order.Id,
            detailsUrl = Url.Action("Details", "Orders", new { area = "Admin", id = order.Id })
                ?? $"/Admin/Orders/Details/{order.Id}",
            customer,
            email = order.CustomerEmail ?? string.Empty,
            initials = Initials(customer),
            createdAt = order.CreatedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            paymentStatus = order.PaymentStatus.ToString(),
            paymentBadge = PaymentStatusBadge(order.PaymentStatus),
            status = order.Status.ToString(),
            statusBadge = OrderStatusBadge(order.Status),
            total = StoreViewHelpers.Money(order.TotalAmount),
            message = _localizer["New order #{0}", order.Id].Value
        };
    }

    private static string Initials(string value)
    {
        var initials = value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part[0])
            .Take(2)
            .Aggregate(string.Empty, (current, ch) => current + ch)
            .ToUpperInvariant();

        return string.IsNullOrWhiteSpace(initials) ? "C" : initials;
    }

    private static string OrderStatusBadge(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Created => "bg-label-secondary",
            OrderStatus.Paid => "bg-label-info",
            OrderStatus.Processing => "bg-label-warning",
            OrderStatus.Completed => "bg-label-success",
            OrderStatus.Cancelled => "bg-label-danger",
            _ => "bg-label-secondary"
        };
    }

    private static string PaymentStatusBadge(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Pending => "bg-label-warning",
            PaymentStatus.Paid => "bg-label-success",
            PaymentStatus.Failed => "bg-label-danger",
            PaymentStatus.Refunded => "bg-label-info",
            _ => "bg-label-secondary"
        };
    }
}
