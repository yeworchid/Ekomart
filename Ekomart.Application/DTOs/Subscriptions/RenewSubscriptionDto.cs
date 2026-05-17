using System.ComponentModel.DataAnnotations;

namespace Ekomart.Application.DTOs.Subscriptions;

public class RenewSubscriptionDto
{
    [Range(1, int.MaxValue)]
    public int SubscriptionPlanId { get; set; }
}
