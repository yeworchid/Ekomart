using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class OrdersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int id)
    {
        return View();
    }
}
