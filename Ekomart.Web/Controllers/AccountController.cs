using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Controllers;

public class AccountController : Controller
{
    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }
}
