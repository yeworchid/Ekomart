using Ekomart.Application.DTOs.Catalog;
using Ekomart.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Ekomart.Web.Models.Store;

namespace Ekomart.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICatalogService _catalogService;

    public HomeController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await _catalogService.GetActiveCategoriesAsync(cancellationToken);
        var products = await _catalogService.GetProductsAsync(
            new CatalogFilterDto
            {
                PageNumber = 1,
                PageSize = 8,
                Sort = "newest"
            },
            cancellationToken);

        return View(new HomeIndexViewModel
        {
            Categories = categories,
            FeaturedProducts = products.Items
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return RedirectToAction("ServerError", "Errors");
    }
}
