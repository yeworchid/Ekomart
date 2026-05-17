using Ekomart.Domain.Enums;

namespace Ekomart.Application.DTOs.Audit;

public class AuditLogDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
