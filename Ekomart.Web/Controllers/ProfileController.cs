using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

public class ProfileController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
