using System.ComponentModel.DataAnnotations;

namespace Ekomart.Application.DTOs.Subscriptions;

public class SubscriptionPlanDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(1, 3660)]
    public int DurationDays { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;
    public IReadOnlyList<SubscriptionFeatureDto> Features { get; set; } = Array.Empty<SubscriptionFeatureDto>();
}
