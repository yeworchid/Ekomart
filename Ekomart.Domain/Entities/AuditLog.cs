using Ekomart.Domain.Common;
using Ekomart.Domain.Enums;

namespace Ekomart.Domain.Entities;

public class AuditLog : Entity
{
    public string? UserId { get; set; }
    public AuditAction Action { get; set; }

    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }

    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}