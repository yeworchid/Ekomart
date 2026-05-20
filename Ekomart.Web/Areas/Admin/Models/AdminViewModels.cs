using System.Globalization;
using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Admin;
using Ekomart.Application.DTOs.Audit;
using Ekomart.Application.DTOs.Catalog;
using Ekomart.Application.DTOs.Orders;
using Ekomart.Application.DTOs.Subscriptions;
using Ekomart.Application.DTOs.Users;
using Ekomart.Domain.Enums;

namespace Ekomart.Web.Areas.Admin.Models;

public class AdminProductsIndexViewModel
{
    public AdminProductFilterDto Filter { get; set; } = new();
    public PagedResult<ProductAdminListItemDto> Products { get; set; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
}

public class AdminProductFormViewModel
{
    public ProductEditDto Product { get; set; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
}

public class AdminCategoriesIndexViewModel
{
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public IReadOnlyDictionary<int, int> ProductCounts { get; set; } = new Dictionary<int, int>();
}

public class AdminCategoryFormViewModel
{
    public CategoryEditDto Category { get; set; } = new();
}

public class AdminOrdersIndexViewModel
{
    public OrderFilterDto Filter { get; set; } = new();
    public PagedResult<OrderDto> Orders { get; set; } = new();
}

public class AdminOrderDetailsViewModel
{
    public OrderDto Order { get; set; } = new();
}

public class AdminSubscriptionPlansIndexViewModel
{
    public PagedRequest Request { get; set; } = new();
    public PagedResult<SubscriptionPlanDto> Plans { get; set; } = new();
}

public class AdminSubscriptionPlanFormViewModel
{
    public SubscriptionPlanDto Plan { get; set; } = new();
    public string? FeaturesText { get; set; }
}

public class AdminUsersIndexViewModel
{
    public PagedRequest Request { get; set; } = new();
    public PagedResult<UserListItemDto> Users { get; set; } = new();
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

public class AdminRolesIndexViewModel
{
    public IReadOnlyList<AdminRoleSummaryViewModel> Roles { get; set; } = Array.Empty<AdminRoleSummaryViewModel>();
}

public class AdminRoleSummaryViewModel
{
    public string Name { get; set; } = string.Empty;
    public int UsersCount { get; set; }
}

public class AdminAuditLogsIndexViewModel
{
    public AuditLogFilterDto Filter { get; set; } = new();
    public PagedResult<AuditLogDto> Logs { get; set; } = new();
}

public class AdminPagerViewModel
{
    public string Action { get; set; } = "Index";
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public Dictionary<string, string> RouteValues { get; set; } = new();
}

public static class AdminViewHelpers
{
    private static readonly CultureInfo RubleCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static string Money(decimal value)
    {
        return value.ToString("C", RubleCulture);
    }

    public static string DateTime(DateTime value)
    {
        return value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    public static string ProductStatusBadge(bool isActive)
    {
        return isActive ? "bg-label-success" : "bg-label-secondary";
    }

    public static string ProductStatusText(bool isActive)
    {
        return isActive ? "Active" : "Disabled";
    }

    public static string StockBadge(int stockQuantity)
    {
        return stockQuantity switch
        {
            <= 0 => "bg-label-danger",
            <= 5 => "bg-label-warning",
            _ => "bg-label-success"
        };
    }

    public static string OrderStatusBadge(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Created => "bg-label-secondary",
            OrderStatus.Paid => "bg-label-info",
            OrderStatus.Processing => "bg-label-warning",
            OrderStatus.Completed => "bg-label-success",
            OrderStatus.Cancelled => "bg-label-danger",
            _ => "bg-label-secondary"
        };
    }

    public static string PaymentStatusBadge(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Pending => "bg-label-warning",
            PaymentStatus.Paid => "bg-label-success",
            PaymentStatus.Failed => "bg-label-danger",
            PaymentStatus.Refunded => "bg-label-info",
            _ => "bg-label-secondary"
        };
    }

    public static string AuditActionBadge(AuditAction action)
    {
        return action switch
        {
            AuditAction.ProductCreated or AuditAction.CategoryCreated or AuditAction.SubscriptionPlanCreated => "bg-label-success",
            AuditAction.ProductUpdated or AuditAction.CategoryUpdated or AuditAction.SubscriptionPlanUpdated or AuditAction.OrderStatusChanged or AuditAction.UserRoleChanged => "bg-label-warning",
            AuditAction.ProductDeleted or AuditAction.CategoryDeleted or AuditAction.SubscriptionPlanDeleted => "bg-label-danger",
            _ => "bg-label-info"
        };
    }
}
