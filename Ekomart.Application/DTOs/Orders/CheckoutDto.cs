using System.ComponentModel.DataAnnotations;

namespace Ekomart.Application.DTOs.Orders;

public class CheckoutDto
{
    [Required]
    [StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(40)]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(160)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string DeliveryAddress { get; set; } = string.Empty;
}
