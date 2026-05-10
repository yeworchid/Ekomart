using Ekomart.Domain.Common;

namespace Ekomart.Domain.Entities;

public class SubscriptionPlan : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SubscriptionFeature> Features { get; set; } = new List<SubscriptionFeature>();
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}