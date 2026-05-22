using Ekomart.Domain.Enums;

namespace Ekomart.Application.DTOs.Orders;

public class OrderDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal ItemsTotal { get; set; }
    public decimal SubscriptionDiscountPercent { get; set; }
    public decimal SubscriptionDiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? DeliveryAddress { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public bool TermsAccepted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public IReadOnlyList<OrderItemDto> Items { get; set; } = Array.Empty<OrderItemDto>();
}
