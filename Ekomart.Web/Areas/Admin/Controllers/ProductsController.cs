using Ekomart.Web.Authorization;
using Ekomart.Application.DTOs.Admin;
using Ekomart.Application.Interfaces;
using Ekomart.Web.Areas.Admin.Models;
using Ekomart.Web.Models.Store;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ManagerOrAdmin)]
public class ProductsController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IWebHostEnvironment _environment;
    private readonly IProductService _productService;

    public ProductsController(
        IProductService productService,
        ICategoryService categoryService,
        IWebHostEnvironment environment)
    {
        _productService = productService;
        _categoryService = categoryService;
        _environment = environment;
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
                image = StoreViewHelpers.ProductImage(product.ImageUrl, product.Id),
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
        NormalizeProductForm(model);

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

        NormalizeProductForm(model);

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

    private void NormalizeProductForm(AdminProductFormViewModel model)
    {
        model.Product.Name = model.Product.Name.Trim();
        model.Product.Slug = model.Product.Slug.Trim();
        model.Product.Description = string.IsNullOrWhiteSpace(model.Product.Description)
            ? null
            : model.Product.Description.Trim();
        model.Product.ImageUrl = NormalizeImageUrl(model.Product.ImageUrl);

        NormalizePrice(model);
        ValidateImageUrl(model.Product.ImageUrl);
    }

    private void NormalizePrice(AdminProductFormViewModel model)
    {
        var rawPrice = Request.Form["Product.Price"].ToString();
        ModelState.Remove("Product.Price");

        if (!TryParsePrice(rawPrice, out var price))
        {
            ModelState.AddModelError("Product.Price", "Enter a valid price.");
            return;
        }

        if (price < 0.01m)
        {
            ModelState.AddModelError("Product.Price", "Price must be greater than 0.");
            return;
        }

        model.Product.Price = price;
    }

    private void ValidateImageUrl(string? imageUrl)
    {
        ModelState.Remove("Product.ImageUrl");

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        if (!imageUrl.StartsWith("/assets/ekomart/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Product.ImageUrl", "Use an Ekomart asset image path.");
            return;
        }

        var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relativePath));
        var webRoot = Path.GetFullPath(_environment.WebRootPath);

        if (!fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
        {
            ModelState.AddModelError("Product.ImageUrl", "Image file was not found.");
        }
    }

    private static string? NormalizeImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var value = imageUrl.Trim();
        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        if (value.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
        {
            return "/assets/ekomart/images/" + value["/images/".Length..];
        }

        if (value.StartsWith("/assets/ecomart/", StringComparison.OrdinalIgnoreCase))
        {
            return "/assets/ekomart/" + value["/assets/ecomart/".Length..];
        }

        return value;
    }

    private static bool TryParsePrice(string rawPrice, out decimal price)
    {
        var normalized = rawPrice.Trim().Replace(',', '.');
        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out price);
    }

    private static string AdminMoney(decimal value)
    {
        return value.ToString("C", CultureInfo.GetCultureInfo("en-US"));
    }
}
