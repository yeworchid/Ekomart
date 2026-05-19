using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

public class CatalogController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(string? slug = null)
    {
        return View();
    }
}
