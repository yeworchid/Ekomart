using Ekomart.Application.DTOs.Admin;
using Ekomart.Application.Interfaces;
using Ekomart.Domain.Enums;
using Ekomart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ekomart.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;

    public DashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardMetricsDto> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return new DashboardMetricsDto
        {
            ProductsCount = await _dbContext.Products.CountAsync(product => product.IsActive, cancellationToken),
            OrdersCount = await _dbContext.Orders.CountAsync(cancellationToken),
            UsersCount = await _dbContext.Users.CountAsync(cancellationToken),
            ActiveSubscriptionsCount = await _dbContext.UserSubscriptions.CountAsync(
                subscription => subscription.Status == SubscriptionStatus.Active && subscription.EndsAtUtc > now,
                cancellationToken),
            OrdersTotalAmount = await _dbContext.Orders
                .Select(order => (decimal?)order.TotalAmount)
                .SumAsync(cancellationToken) ?? 0,
            AuditLogsCount = await _dbContext.AuditLogs.CountAsync(cancellationToken)
        };
    }
}
