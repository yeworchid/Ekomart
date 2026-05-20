using Ekomart.Web.Authorization;
using System.Text.RegularExpressions;
using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Subscriptions;
using Ekomart.Application.Interfaces;
using Ekomart.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomart.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ManagerOrAdmin)]
public class SubscriptionsController : Controller
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    public async Task<IActionResult> Index(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        return View(new AdminSubscriptionPlansIndexViewModel
        {
            Request = request,
            Plans = await _subscriptionService.GetPlansForAdminAsync(request, cancellationToken)
        });
    }

    public IActionResult Create()
    {
        return View(new AdminSubscriptionPlanFormViewModel
        {
            Plan = new SubscriptionPlanDto
            {
                DurationDays = 30,
                IsActive = true
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdminSubscriptionPlanFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.Plan.Features = ParseFeatures(model.FeaturesText);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var id = await _subscriptionService.CreatePlanAsync(model.Plan, CurrentUserId(), cancellationToken);
            TempData["Success"] = "Subscription plan created.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var plan = await _subscriptionService.GetPlanAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound();
        }

        return View(new AdminSubscriptionPlanFormViewModel
        {
            Plan = plan,
            FeaturesText = ToFeaturesText(plan.Features)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        AdminSubscriptionPlanFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Plan.Id)
        {
            return NotFound();
        }

        model.Plan.Features = ParseFeatures(model.FeaturesText);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _subscriptionService.UpdatePlanAsync(model.Plan, CurrentUserId(), cancellationToken);
            TempData["Success"] = "Subscription plan updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _subscriptionService.DeletePlanAsync(id, CurrentUserId(), cancellationToken);
        TempData["Success"] = "Subscription plan disabled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        await _subscriptionService.ActivatePlanAsync(id, CurrentUserId(), cancellationToken);
        TempData["Success"] = "Subscription plan activated.";
        return RedirectToAction(nameof(Index));
    }

    private static IReadOnlyList<SubscriptionFeatureDto> ParseFeatures(string? featuresText)
    {
        if (string.IsNullOrWhiteSpace(featuresText))
        {
            return Array.Empty<SubscriptionFeatureDto>();
        }

        return featuresText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var parts = line.Split('|', StringSplitOptions.TrimEntries);
                var name = parts.ElementAtOrDefault(0) ?? string.Empty;
                var code = parts.ElementAtOrDefault(1);

                return new SubscriptionFeatureDto
                {
                    Name = name,
                    Code = string.IsNullOrWhiteSpace(code) ? Slugify(name) : code,
                    Description = parts.ElementAtOrDefault(2)
                };
            })
            .Where(feature => !string.IsNullOrWhiteSpace(feature.Name))
            .ToArray();
    }

    private static string ToFeaturesText(IReadOnlyList<SubscriptionFeatureDto> features)
    {
        return string.Join(
            Environment.NewLine,
            features.Select(feature => $"{feature.Name}|{feature.Code}|{feature.Description}"));
    }

    private static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var slug = Regex.Replace(lower, "[^a-z0-9а-яё]+", "-");
        return slug.Trim('-');
    }

    private string CurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
