using Ekomart.Domain.Common;
using Ekomart.Domain.Enums;

namespace Ekomart.Domain.Entities;

public class UserSubscription : Entity
{
    public string UserId { get; set; } = string.Empty;

    public int SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
}