using Ekomart.Application.DTOs.Catalog;
using Ekomart.Application.Interfaces;
using Ekomart.Web.Models.Store;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

public class CatalogController : Controller
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IActionResult> Index(
        [FromQuery] CatalogFilterDto filter,
        CancellationToken cancellationToken)
    {
        filter.PageSize = filter.PageSize <= 0 ? 12 : filter.PageSize;

        var products = await _catalogService.GetProductsAsync(filter, cancellationToken);
        var categories = await _catalogService.GetActiveCategoriesAsync(cancellationToken);

        var model = new CatalogIndexViewModel
        {
            Filter = filter,
            Categories = categories,
            Products = products
        };

        if (IsAjaxRequest())
        {
            return PartialView("_CatalogResults", model);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CategoriesMenu(CancellationToken cancellationToken)
    {
        var categories = await _catalogService.GetActiveCategoriesAsync(cancellationToken);

        return Json(categories.Select(category => new
        {
            category.Name,
            category.Slug,
            Icon = StoreViewHelpers.CategoryIcon(category.Slug),
            Href = Url.Action(nameof(Index), "Catalog", new { categorySlug = category.Slug }) ?? "/Catalog"
        }));
    }

    public async Task<IActionResult> Details(
        string? slug = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return RedirectToAction(nameof(Index));
        }

        var product = await _catalogService.GetProductBySlugAsync(slug, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return View(new ProductDetailsViewModel
        {
            Product = product,
            Categories = await _catalogService.GetActiveCategoriesAsync(cancellationToken),
            RelatedProducts = await _catalogService.GetRelatedProductsAsync(
                product.Id,
                product.CategoryId,
                cancellationToken: cancellationToken)
        });
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"].ToString(),
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }
}
