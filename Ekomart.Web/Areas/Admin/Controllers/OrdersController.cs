using Ekomart.Web.Authorization;
using Ekomart.Application.DTOs.Orders;
using Ekomart.Application.Interfaces;
using Ekomart.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ManagerOrAdmin)]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IActionResult> Index(
        [FromQuery] OrderFilterDto filter,
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersForAdminAsync(filter, cancellationToken);

        return View(new AdminOrdersIndexViewModel
        {
            Filter = filter,
            Orders = orders
        });
    }

    public async Task<IActionResult> Data(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrdersForAdminAsync(
            new OrderFilterDto
            {
                PageSize = 100
            },
            cancellationToken);

        return Json(new
        {
            data = orders.Items.Select(order => new
            {
                id = order.Id,
                order = order.Id,
                date = order.CreatedAtUtc.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                time = order.CreatedAtUtc.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                customer = order.CustomerName ?? "Customer",
                email = order.CustomerEmail ?? string.Empty,
                avatar = (string?)null,
                payment = PaymentStatusCode(order.PaymentStatus),
                status = OrderStatusCode(order.Status),
                method = "mastercard",
                method_number = order.TotalAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            })
        });
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderAsync(id, cancellationToken: cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        return View(new AdminOrderDetailsViewModel
        {
            Order = order
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        int id,
        UpdateOrderStatusDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            await _orderService.ChangeStatusAsync(id, dto, CurrentUserId(), cancellationToken);
            TempData["Success"] = "Order status updated.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private string CurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private static int OrderStatusCode(Ekomart.Domain.Enums.OrderStatus status)
    {
        return status switch
        {
            Ekomart.Domain.Enums.OrderStatus.Completed => 2,
            Ekomart.Domain.Enums.OrderStatus.Processing => 3,
            Ekomart.Domain.Enums.OrderStatus.Cancelled => 5,
            Ekomart.Domain.Enums.OrderStatus.Created => 4,
            _ => 1
        };
    }

    private static int PaymentStatusCode(Ekomart.Domain.Enums.PaymentStatus status)
    {
        return status switch
        {
            Ekomart.Domain.Enums.PaymentStatus.Pending => 2,
            Ekomart.Domain.Enums.PaymentStatus.Failed => 3,
            Ekomart.Domain.Enums.PaymentStatus.Refunded => 4,
            _ => 1
        };
    }
}
