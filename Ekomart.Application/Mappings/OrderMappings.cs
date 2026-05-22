using Ekomart.Application.DTOs.Orders;
using Ekomart.Domain.Entities;

namespace Ekomart.Application.Mappings;

public static class OrderMappings
{
    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            ItemsTotal = order.ItemsTotal,
            SubscriptionDiscountPercent = order.SubscriptionDiscountPercent,
            SubscriptionDiscountAmount = order.SubscriptionDiscountAmount,
            TotalAmount = order.TotalAmount,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            CustomerEmail = order.CustomerEmail,
            DeliveryAddress = order.DeliveryAddress,
            DeliveryMethod = order.DeliveryMethod,
            PaymentMethod = order.PaymentMethod,
            TermsAccepted = order.TermsAccepted,
            CreatedAtUtc = order.CreatedAtUtc,
            Items = order.Items.Select(item => item.ToDto()).ToArray()
        };
    }

    public static OrderItemDto ToDto(this OrderItem orderItem)
    {
        return new OrderItemDto
        {
            ProductId = orderItem.ProductId,
            ProductName = orderItem.ProductName,
            UnitPrice = orderItem.UnitPrice,
            Quantity = orderItem.Quantity,
            LineTotal = orderItem.LineTotal
        };
    }
}
