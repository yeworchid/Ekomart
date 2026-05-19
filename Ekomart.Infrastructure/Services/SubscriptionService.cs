using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Subscriptions;
using Ekomart.Application.Interfaces;
using Ekomart.Application.Mappings;
using Ekomart.Domain.Entities;
using Ekomart.Domain.Enums;
using Ekomart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ekomart.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _dbContext;

    public SubscriptionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetActivePlansAsync(
        CancellationToken cancellationToken = default)
    {
        var plans = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Include(plan => plan.Features)
            .Where(plan => plan.IsActive)
            .OrderBy(plan => plan.Price)
            .ThenBy(plan => plan.Name)
            .ToListAsync(cancellationToken);

        return plans.Select(plan => plan.ToDto()).ToArray();
    }

    public async Task<UserSubscriptionDto?> GetCurrentUserSubscriptionAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var subscription = await _dbContext.UserSubscriptions
            .AsNoTracking()
            .Include(item => item.SubscriptionPlan)
            .Where(item =>
                item.UserId == userId &&
                item.Status == SubscriptionStatus.Active &&
                item.EndsAtUtc > now)
            .OrderByDescending(item => item.EndsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return subscription?.ToDto();
    }

    public async Task<UserSubscriptionDto> RenewAsync(
        string userId,
        RenewSubscriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans
            .Include(item => item.Features)
            .FirstOrDefaultAsync(
                item => item.Id == dto.SubscriptionPlanId && item.IsActive,
                cancellationToken);

        if (plan is null)
        {
            throw new InvalidOperationException("Subscription plan was not found.");
        }

        var now = DateTime.UtcNow;
        var subscription = await _dbContext.UserSubscriptions
            .Include(item => item.SubscriptionPlan)
            .Where(item => item.UserId == userId && item.Status == SubscriptionStatus.Active)
            .OrderByDescending(item => item.EndsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription is null)
        {
            subscription = new UserSubscription
            {
                UserId = userId,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlan = plan,
                StartsAtUtc = now,
                EndsAtUtc = now.AddDays(plan.DurationDays),
                Status = SubscriptionStatus.Active
            };

            _dbContext.UserSubscriptions.Add(subscription);
        }
        else
        {
            var renewalStart = subscription.EndsAtUtc > now ? subscription.EndsAtUtc : now;
            subscription.SubscriptionPlanId = plan.Id;
            subscription.SubscriptionPlan = plan;
            subscription.StartsAtUtc = subscription.StartsAtUtc == default ? now : subscription.StartsAtUtc;
            subscription.EndsAtUtc = renewalStart.AddDays(plan.DurationDays);
            subscription.Status = SubscriptionStatus.Active;
            subscription.UpdatedAtUtc = now;
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = AuditAction.SubscriptionRenewed,
            EntityName = nameof(UserSubscription),
            Details = $"Subscription renewed: {plan.Name}."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return subscription.ToDto();
    }

    public async Task<PagedResult<SubscriptionPlanDto>> GetPlansForAdminAsync(
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Include(plan => plan.Features)
            .OrderBy(plan => plan.Name);

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Paging.PageNumber(request);
        var pageSize = Paging.PageSize(request);

        var plans = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Paging.Result(
            plans.Select(plan => plan.ToDto()).ToArray(),
            request,
            totalCount);
    }

    public async Task<SubscriptionPlanDto?> GetPlanAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Include(item => item.Features)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return plan?.ToDto();
    }

    public async Task<int> CreatePlanAsync(
        SubscriptionPlanDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var plan = new SubscriptionPlan
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description,
            Price = dto.Price,
            DurationDays = dto.DurationDays,
            DiscountPercent = dto.DiscountPercent,
            IsActive = dto.IsActive,
            Features = dto.Features
                .Select(feature => new SubscriptionFeature
                {
                    Name = feature.Name,
                    Code = feature.Code,
                    Description = feature.Description
                })
                .ToList()
        };

        _dbContext.SubscriptionPlans.Add(plan);
        AddAudit(adminUserId, AuditAction.SubscriptionPlanCreated, nameof(SubscriptionPlan), null, $"Plan created: {dto.Name}.");
        await _dbContext.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }

    public async Task UpdatePlanAsync(
        SubscriptionPlanDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans
            .Include(item => item.Features)
            .FirstOrDefaultAsync(item => item.Id == dto.Id, cancellationToken);

        if (plan is null)
        {
            throw new InvalidOperationException("Subscription plan was not found.");
        }

        dto.ApplyTo(plan);
        _dbContext.SubscriptionFeatures.RemoveRange(plan.Features);

        plan.Features = dto.Features
            .Select(feature => new SubscriptionFeature
            {
                SubscriptionPlanId = plan.Id,
                Name = feature.Name,
                Code = feature.Code,
                Description = feature.Description
            })
            .ToList();

        AddAudit(adminUserId, AuditAction.SubscriptionPlanUpdated, nameof(SubscriptionPlan), plan.Id.ToString(), $"Plan updated: {dto.Name}.");
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePlanAsync(
        int id,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (plan is null)
        {
            return;
        }

        plan.IsActive = false;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        AddAudit(adminUserId, AuditAction.SubscriptionPlanDeleted, nameof(SubscriptionPlan), plan.Id.ToString(), $"Plan disabled: {plan.Name}.");
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AddAudit(
        string userId,
        AuditAction action,
        string entityName,
        string? entityId,
        string details)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details
        });
    }
}
