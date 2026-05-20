using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Subscriptions;

namespace Ekomart.Application.Interfaces;

public interface ISubscriptionService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> GetActivePlansAsync(
        CancellationToken cancellationToken = default);

    Task<UserSubscriptionDto?> GetCurrentUserSubscriptionAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserSubscriptionDto> RenewAsync(
        string userId,
        RenewSubscriptionDto dto,
        CancellationToken cancellationToken = default);

    Task<PagedResult<SubscriptionPlanDto>> GetPlansForAdminAsync(
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPlanDto?> GetPlanAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<int> CreatePlanAsync(
        SubscriptionPlanDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task UpdatePlanAsync(
        SubscriptionPlanDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task DeletePlanAsync(
        int id,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task ActivatePlanAsync(
        int id,
        string adminUserId,
        CancellationToken cancellationToken = default);
}
