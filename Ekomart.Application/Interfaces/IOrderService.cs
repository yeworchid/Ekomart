using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Orders;

namespace Ekomart.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CheckoutAsync(
        string userId,
        CheckoutDto dto,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OrderDto>> GetUserOrdersAsync(
        string userId,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDto?> GetOrderAsync(
        int orderId,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OrderDto>> GetOrdersForAdminAsync(
        OrderFilterDto filter,
        CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(
        int orderId,
        UpdateOrderStatusDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default);
}
