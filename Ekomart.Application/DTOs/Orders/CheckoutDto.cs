using System.ComponentModel.DataAnnotations;
using Ekomart.Domain.Enums;

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

    [Required(ErrorMessage = "Select a delivery method.")]
    public DeliveryMethod? DeliveryMethod { get; set; }

    [Required(ErrorMessage = "Select a payment method.")]
    public PaymentMethod? PaymentMethod { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Accept the terms and conditions.")]
    public bool TermsAccepted { get; set; }
}
