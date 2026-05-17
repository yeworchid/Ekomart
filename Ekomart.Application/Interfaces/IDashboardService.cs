using Ekomart.Application.DTOs.Admin;

namespace Ekomart.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetMetricsAsync(CancellationToken cancellationToken = default);
}
