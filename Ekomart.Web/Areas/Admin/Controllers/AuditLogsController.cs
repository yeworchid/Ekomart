using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AuditLogsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
