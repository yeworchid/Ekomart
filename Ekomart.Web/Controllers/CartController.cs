using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

public class CartController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
