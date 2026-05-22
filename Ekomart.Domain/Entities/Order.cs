using Ekomart.Domain.Common;
using Ekomart.Domain.Enums;

namespace Ekomart.Domain.Entities;

public class Order : Entity
{
    public string UserId { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public decimal ItemsTotal { get; set; }
    public decimal SubscriptionDiscountPercent { get; set; }
    public decimal SubscriptionDiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? DeliveryAddress { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.FreeShipping;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.DirectBankTransfer;
    public bool TermsAccepted { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
