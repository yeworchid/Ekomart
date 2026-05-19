using Ekomart.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ManagerOrAdmin)]
public class AuditLogsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
