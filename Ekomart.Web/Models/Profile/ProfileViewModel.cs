using System.ComponentModel.DataAnnotations;
using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Orders;
using Ekomart.Application.DTOs.Subscriptions;

namespace Ekomart.Web.Models.Profile;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Введите имя.")]
    [StringLength(120, ErrorMessage = "Имя не должно быть длиннее 120 символов.")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public PagedResult<OrderDto> Orders { get; set; } = new();
    public UserSubscriptionDto? CurrentSubscription { get; set; }
    public IReadOnlyList<SubscriptionPlanDto> SubscriptionPlans { get; set; } = Array.Empty<SubscriptionPlanDto>();
}
