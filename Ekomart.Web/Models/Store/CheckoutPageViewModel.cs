using Ekomart.Application.DTOs.Cart;
using Ekomart.Application.DTOs.Orders;
using Ekomart.Application.DTOs.Subscriptions;

namespace Ekomart.Web.Models.Store;

public class CheckoutPageViewModel
{
    public CartDto Cart { get; set; } = new();
    public CheckoutDto Checkout { get; set; } = new();
    public UserSubscriptionDto? CurrentSubscription { get; set; }
    public IReadOnlyList<SubscriptionPlanDto> Plans { get; set; } = Array.Empty<SubscriptionPlanDto>();
}
