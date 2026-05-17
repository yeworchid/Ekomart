using Ekomart.Domain.Enums;

namespace Ekomart.Application.DTOs.Orders;

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}
