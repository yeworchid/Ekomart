using Ekomart.Domain.Enums;

namespace Ekomart.Application.DTOs.Subscriptions;

public class UserSubscriptionDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int SubscriptionPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public SubscriptionStatus Status { get; set; }
}
