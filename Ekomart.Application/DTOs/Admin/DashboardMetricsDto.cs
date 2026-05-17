namespace Ekomart.Application.DTOs.Admin;

public class DashboardMetricsDto
{
    public int ProductsCount { get; set; }
    public int OrdersCount { get; set; }
    public int UsersCount { get; set; }
    public int ActiveSubscriptionsCount { get; set; }
    public decimal OrdersTotalAmount { get; set; }
    public int AuditLogsCount { get; set; }
}
