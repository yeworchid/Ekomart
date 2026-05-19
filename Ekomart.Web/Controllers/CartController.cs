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
            await _cartService.AddItemAsync(GetUserId(), productId, quantity, cancellationToken);
            TempData["CartMessage"] = "Товар добавлен в корзину.";
        }
        catch (InvalidOperationException exception)
        {
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
            TempData["CartError"] = "Некорректное количество товара.";
            return RedirectToAction(nameof(Index));
        }

        await _cartService.UpdateQuantityAsync(GetUserId(), dto, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        int productId,
        CancellationToken cancellationToken)
    {
        await _cartService.RemoveItemAsync(GetUserId(), productId, cancellationToken);
        return RedirectToAction(nameof(Index));
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
}
