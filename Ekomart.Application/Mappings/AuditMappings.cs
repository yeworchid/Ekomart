using Ekomart.Application.DTOs.Audit;
using Ekomart.Domain.Entities;

namespace Ekomart.Application.Mappings;

public static class AuditMappings
{
    public static AuditLogDto ToDto(this AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            Action = auditLog.Action,
            EntityName = auditLog.EntityName,
            EntityId = auditLog.EntityId,
            Details = auditLog.Details,
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            CreatedAtUtc = auditLog.CreatedAtUtc
        };
    }
}
