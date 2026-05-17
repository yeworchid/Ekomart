using System.ComponentModel.DataAnnotations;
using Ekomart.Application.Common;
using Ekomart.Domain.Enums;

namespace Ekomart.Application.DTOs.Orders;

public class OrderFilterDto : PagedRequest
{
    [StringLength(450)]
    public string? UserId { get; set; }

    public OrderStatus? Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
