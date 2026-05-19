using Ekomart.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.AdminOnly)]
public class RolesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
