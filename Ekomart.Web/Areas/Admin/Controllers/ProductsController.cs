using Ekomart.Web.Authorization;
using Ekomart.Application.DTOs.Admin;
using Ekomart.Application.Interfaces;
using Ekomart.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ManagerOrAdmin)]
public class ProductsController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;

    public ProductsController(
        IProductService productService,
        ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(
        [FromQuery] AdminProductFilterDto filter,
        CancellationToken cancellationToken)
    {
        var products = await _productService.GetProductsForAdminAsync(filter, cancellationToken);
        var categories = await _categoryService.GetCategoriesAsync(cancellationToken);

        return View(new AdminProductsIndexViewModel
        {
            Filter = filter,
            Products = products,
            Categories = categories
        });
    }

    public async Task<IActionResult> Data(CancellationToken cancellationToken)
    {
        var products = await _productService.GetProductsForAdminAsync(
            new AdminProductFilterDto
            {
                PageSize = 100
            },
            cancellationToken);

        return Json(new
        {
            data = products.Items.Select(product => new
            {
                id = product.Id,
                product_name = product.Name,
                product_brand = product.CategoryName,
                image = (string?)null,
                category = product.CategoryName,
                stock = product.StockQuantity > 0 ? 1 : 0,
                sku = product.Slug,
                price = AdminMoney(product.Price),
                qty = product.StockQuantity,
                quantity = product.StockQuantity,
                status = product.IsActive ? 2 : 3
            })
        });
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetCategoriesAsync(cancellationToken);

        return View(new AdminProductFormViewModel
        {
            Categories = categories,
            Product = new ProductEditDto
            {
                CategoryId = categories.FirstOrDefault()?.Id ?? 0,
                IsActive = true
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdminProductFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await FillCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var id = await _productService.CreateProductAsync(model.Product, CurrentUserId(), cancellationToken);
            TempData["Success"] = "Product created.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await FillCategoriesAsync(model, cancellationToken);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductForEditAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return View(new AdminProductFormViewModel
        {
            Product = product,
            Categories = await _categoryService.GetCategoriesAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        AdminProductFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Product.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await FillCategoriesAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            await _productService.UpdateProductAsync(model.Product, CurrentUserId(), cancellationToken);
            TempData["Success"] = "Product updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await FillCategoriesAsync(model, cancellationToken);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _productService.DeleteProductAsync(id, CurrentUserId(), cancellationToken);
        TempData["Success"] = "Product disabled.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillCategoriesAsync(
        AdminProductFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.Categories = await _categoryService.GetCategoriesAsync(cancellationToken);
    }

    private string CurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private static string AdminMoney(decimal value)
    {
        return value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));
    }
}
