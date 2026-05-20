using Ekomart.Web.Authorization;
using Ekomart.Application.DTOs.Admin;
using Ekomart.Application.Interfaces;
using Ekomart.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ManagerOrAdmin)]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;

    public CategoriesController(
        ICategoryService categoryService,
        IProductService productService)
    {
        _categoryService = categoryService;
        _productService = productService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new AdminCategoriesIndexViewModel
        {
            Categories = await _categoryService.GetCategoriesAsync(cancellationToken)
        });
    }

    public async Task<IActionResult> Data(CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetCategoriesAsync(cancellationToken);
        var rows = new List<object>(categories.Count);

        foreach (var category in categories)
        {
            var products = await _productService.GetProductsForAdminAsync(
                new AdminProductFilterDto
                {
                    CategoryId = category.Id,
                    PageSize = 1
                },
                cancellationToken);

            rows.Add(new
            {
                id = category.Id,
                categories = category.Name,
                category_detail = category.Description ?? category.Slug,
                cat_image = (string?)null,
                total_products = products.TotalCount,
                total_earnings = category.IsActive ? "Active" : "Disabled"
            });
        }

        return Json(new { data = rows });
    }

    public IActionResult Create()
    {
        return View(new AdminCategoryFormViewModel
        {
            Category = new CategoryEditDto
            {
                IsActive = true
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdminCategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _categoryService.CreateCategoryAsync(model.Category, CurrentUserId(), cancellationToken);
            TempData["Success"] = "Category created.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetCategoryForEditAsync(id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return View(new AdminCategoryFormViewModel
        {
            Category = category
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        AdminCategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Category.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _categoryService.UpdateCategoryAsync(model.Category, CurrentUserId(), cancellationToken);
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteCategoryAsync(id, CurrentUserId(), cancellationToken);
        TempData["Success"] = "Category disabled.";
        return RedirectToAction(nameof(Index));
    }

    private string CurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
