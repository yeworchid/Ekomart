using System.ComponentModel.DataAnnotations;
using Ekomart.Domain.Enums;

namespace Ekomart.Application.DTOs.Orders;

public class UpdateOrderStatusDto
{
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Status { get; set; }
}
