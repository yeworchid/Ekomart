using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Audit;
using Ekomart.Domain.Enums;

namespace Ekomart.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        AuditAction action,
        string entityName,
        string? entityId = null,
        string? userId = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogDto>> GetLogsAsync(
        AuditLogFilterDto filter,
        CancellationToken cancellationToken = default);
}
