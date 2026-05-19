using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

public class OrdersController : Controller
{
    public IActionResult Checkout()
    {
        return View();
    }
}
