using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class UsersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
