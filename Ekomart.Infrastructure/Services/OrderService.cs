using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Orders;
using Ekomart.Application.Interfaces;
using Ekomart.Application.Mappings;
using Ekomart.Domain.Entities;
using Ekomart.Domain.Enums;
using Ekomart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ekomart.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _dbContext;

    public OrderService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderDto> CheckoutAsync(
        string userId,
        CheckoutDto dto,
        CancellationToken cancellationToken = default)
    {
        var cartItems = await _dbContext.CartItems
            .Include(item => item.Product)
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);

        var validCartItems = cartItems
            .Where(item => item.Product is not null && item.Product.IsActive && item.Quantity > 0)
            .ToArray();

        if (validCartItems.Length == 0)
        {
            throw new InvalidOperationException("Cart is empty.");
        }

        var now = DateTime.UtcNow;
        var activeSubscription = await _dbContext.UserSubscriptions
            .Include(subscription => subscription.SubscriptionPlan)
            .Where(subscription =>
                subscription.UserId == userId &&
                subscription.Status == SubscriptionStatus.Active &&
                subscription.EndsAtUtc > now &&
                subscription.SubscriptionPlan != null &&
                subscription.SubscriptionPlan.IsActive)
            .OrderByDescending(subscription => subscription.EndsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var itemsTotal = validCartItems.Sum(item => item.Product!.Price * item.Quantity);
        var discountPercent = activeSubscription?.SubscriptionPlan?.DiscountPercent ?? 0;
        var discountAmount = Math.Round(itemsTotal * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Paid,
            PaymentStatus = PaymentStatus.Paid,
            ItemsTotal = itemsTotal,
            SubscriptionDiscountPercent = discountPercent,
            SubscriptionDiscountAmount = discountAmount,
            TotalAmount = itemsTotal - discountAmount,
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            CustomerEmail = dto.CustomerEmail,
            DeliveryAddress = dto.DeliveryAddress,
            Items = validCartItems
                .Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product!.Name,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity,
                    LineTotal = item.Product.Price * item.Quantity
                })
                .ToList()
        };

        _dbContext.Orders.Add(order);
        _dbContext.CartItems.RemoveRange(cartItems);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.OrderCreated,
            EntityName = nameof(Order),
            Details = $"Order checkout total: {order.TotalAmount:0.00}"
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }

    public async Task<PagedResult<OrderDto>> GetUserOrdersAsync(
        string userId,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAtUtc);

        return await ToPagedOrders(query, request, cancellationToken);
    }

    public async Task<OrderDto?> GetOrderAsync(
        int orderId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.Id == orderId);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(order => order.UserId == userId);
        }

        var order = await query.FirstOrDefaultAsync(cancellationToken);
        return order?.ToDto();
    }

    public async Task<PagedResult<OrderDto>> GetOrdersForAdminAsync(
        OrderFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.UserId))
        {
            query = query.Where(order => order.UserId == filter.UserId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(order => order.Status == filter.Status.Value);
        }

        if (filter.PaymentStatus.HasValue)
        {
            query = query.Where(order => order.PaymentStatus == filter.PaymentStatus.Value);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(order => order.CreatedAtUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(order => order.CreatedAtUtc <= filter.ToUtc.Value);
        }

        return await ToPagedOrders(
            query.OrderByDescending(order => order.CreatedAtUtc),
            filter,
            cancellationToken);
    }

    public async Task ChangeStatusAsync(
        int orderId,
        UpdateOrderStatusDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("Order was not found.");
        }

        order.Status = dto.Status;
        order.UpdatedAtUtc = DateTime.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = AuditAction.OrderStatusChanged,
            EntityName = nameof(Order),
            EntityId = order.Id.ToString(),
            Details = $"Order status changed to {dto.Status}."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<PagedResult<OrderDto>> ToPagedOrders(
        IQueryable<Order> query,
        PagedRequest request,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Paging.PageNumber(request);
        var pageSize = Paging.PageSize(request);

        var orders = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Paging.Result(
            orders.Select(order => order.ToDto()).ToArray(),
            request,
            totalCount);
    }
}
