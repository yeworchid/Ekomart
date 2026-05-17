using Ekomart.Application.DTOs.Subscriptions;
using Ekomart.Domain.Entities;

namespace Ekomart.Application.Mappings;

public static class SubscriptionMappings
{
    public static SubscriptionPlanDto ToDto(this SubscriptionPlan plan)
    {
        return new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Slug = plan.Slug,
            Description = plan.Description,
            Price = plan.Price,
            DurationDays = plan.DurationDays,
            DiscountPercent = plan.DiscountPercent,
            IsActive = plan.IsActive,
            Features = plan.Features.Select(feature => feature.ToDto()).ToArray()
        };
    }

    public static SubscriptionFeatureDto ToDto(this SubscriptionFeature feature)
    {
        return new SubscriptionFeatureDto
        {
            Id = feature.Id,
            Name = feature.Name,
            Code = feature.Code,
            Description = feature.Description
        };
    }

    public static UserSubscriptionDto ToDto(this UserSubscription subscription)
    {
        return new UserSubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            SubscriptionPlanId = subscription.SubscriptionPlanId,
            PlanName = subscription.SubscriptionPlan?.Name ?? string.Empty,
            StartsAtUtc = subscription.StartsAtUtc,
            EndsAtUtc = subscription.EndsAtUtc,
            Status = subscription.Status
        };
    }

    public static void ApplyTo(this SubscriptionPlanDto dto, SubscriptionPlan plan)
    {
        plan.Name = dto.Name;
        plan.Slug = dto.Slug;
        plan.Description = dto.Description;
        plan.Price = dto.Price;
        plan.DurationDays = dto.DurationDays;
        plan.DiscountPercent = dto.DiscountPercent;
        plan.IsActive = dto.IsActive;
        plan.UpdatedAtUtc = DateTime.UtcNow;
    }
}
