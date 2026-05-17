using System.ComponentModel.DataAnnotations;
using Ekomart.Application.Common;
using Ekomart.Domain.Enums;

namespace Ekomart.Application.DTOs.Audit;

public class AuditLogFilterDto : PagedRequest
{
    [StringLength(450)]
    public string? UserId { get; set; }

    public AuditAction? Action { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
