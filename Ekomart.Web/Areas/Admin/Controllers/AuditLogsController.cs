using Ekomart.Web.Authorization;
using Ekomart.Application.DTOs.Audit;
using Ekomart.Application.Interfaces;
using Ekomart.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ManagerOrAdmin)]
public class AuditLogsController : Controller
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(
        [FromQuery] AuditLogFilterDto filter,
        CancellationToken cancellationToken)
    {
        var logs = await _auditService.GetLogsAsync(filter, cancellationToken);

        return View(new AdminAuditLogsIndexViewModel
        {
            Filter = filter,
            Logs = logs
        });
    }
}
