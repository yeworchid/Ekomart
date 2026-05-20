using Ekomart.Application.DTOs.Cart;
using Ekomart.Application.Interfaces;
using Ekomart.Infrastructure.Identity;
using Ekomart.Web.Models.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(
        ICartService cartService,
        UserManager<ApplicationUser> userManager)
    {
        _cartService = cartService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new CartPageViewModel
        {
            Cart = await _cartService.GetCartAsync(GetUserId(), cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        int productId,
        int quantity = 1,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cart = await _cartService.AddItemAsync(GetUserId(), productId, quantity, cancellationToken);
            if (IsAjaxRequest())
            {
                return Json(CartResponse(cart, "Товар добавлен в корзину."));
            }

            TempData["CartMessage"] = "Товар добавлен в корзину.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new
                {
                    success = false,
                    message = exception.Message
                });
            }

            TempData["CartError"] = exception.Message;
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        UpdateCartItemDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Некорректное количество товара."
                });
            }

            TempData["CartError"] = "Некорректное количество товара.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var cart = await _cartService.UpdateQuantityAsync(GetUserId(), dto, cancellationToken);
            if (IsAjaxRequest())
            {
                return Json(CartResponse(cart, "Корзина обновлена."));
            }
        }
        catch (InvalidOperationException exception)
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new
                {
                    success = false,
                    message = exception.Message
                });
            }

            TempData["CartError"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        int productId,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.RemoveItemAsync(GetUserId(), productId, cancellationToken);
        if (IsAjaxRequest())
        {
            return Json(CartResponse(cart, "Товар удалён из корзины."));
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Count(CancellationToken cancellationToken)
    {
        var totalQuantity = await _cartService.GetTotalQuantityAsync(GetUserId(), cancellationToken);

        return Json(new
        {
            success = true,
            totalQuantity
        });
    }

    private string GetUserId()
    {
        return _userManager.GetUserId(User)
            ?? throw new InvalidOperationException("Authenticated user id was not found.");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"].ToString(),
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }

    private static object CartResponse(CartDto cart, string? message = null)
    {
        return new
        {
            success = true,
            message,
            totalQuantity = cart.TotalQuantity,
            itemsTotal = cart.ItemsTotal,
            itemsTotalText = StoreViewHelpers.Money(cart.ItemsTotal),
            isEmpty = cart.Items.Count == 0,
            items = cart.Items.Select(item => new
            {
                productId = item.ProductId,
                quantity = item.Quantity,
                lineTotal = item.LineTotal,
                lineTotalText = StoreViewHelpers.Money(item.LineTotal)
            })
        };
    }
}
