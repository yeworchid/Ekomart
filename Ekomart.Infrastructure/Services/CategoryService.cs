using Ekomart.Application.DTOs.Admin;
using Ekomart.Application.DTOs.Catalog;
using Ekomart.Application.Interfaces;
using Ekomart.Application.Mappings;
using Ekomart.Domain.Entities;
using Ekomart.Domain.Enums;
using Ekomart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ekomart.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _dbContext;

    public CategoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(category => category.ToDto()).ToArray();
    }

    public async Task<CategoryEditDto?> GetCategoryForEditAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return category?.ToEditDto();
    }

    public async Task<int> CreateCategoryAsync(
        CategoryEditDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var category = new Category();
        dto.ApplyTo(category);
        category.CreatedAtUtc = DateTime.UtcNow;
        category.UpdatedAtUtc = null;

        _dbContext.Categories.Add(category);
        AddAudit(adminUserId, AuditAction.CategoryCreated, category, $"Category created: {category.Name}.");
        await _dbContext.SaveChangesAsync(cancellationToken);

        return category.Id;
    }

    public async Task UpdateCategoryAsync(
        CategoryEditDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(item => item.Id == dto.Id, cancellationToken);

        if (category is null)
        {
            throw new InvalidOperationException("Category was not found.");
        }

        dto.ApplyTo(category);
        AddAudit(adminUserId, AuditAction.CategoryUpdated, category, $"Category updated: {category.Name}.");
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCategoryAsync(
        int id,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (category is null)
        {
            return;
        }

        category.IsActive = false;
        category.UpdatedAtUtc = DateTime.UtcNow;

        AddAudit(adminUserId, AuditAction.CategoryDeleted, category, $"Category disabled: {category.Name}.");
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AddAudit(
        string userId,
        AuditAction action,
        Category category,
        string details)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = nameof(Category),
            EntityId = category.Id == 0 ? null : category.Id.ToString(),
            Details = details
        });
    }
}
