using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Catalog;
using Ekomart.Application.Interfaces;
using Ekomart.Application.Mappings;
using Ekomart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ekomart.Infrastructure.Services;

public class CatalogService : ICatalogService
{
    private readonly AppDbContext _dbContext;

    public CatalogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(
        CatalogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.IsActive)
            .Where(product => product.Category != null && product.Category.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = $"%{filter.Search.Trim()}%";
            query = query.Where(product =>
                EF.Functions.ILike(product.Name, search) ||
                (product.Description != null && EF.Functions.ILike(product.Description, search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            var categorySlug = filter.CategorySlug.Trim();
            query = query.Where(product => product.Category != null && product.Category.Slug == categorySlug);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(product => product.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(product => product.Price <= filter.MaxPrice.Value);
        }

        query = filter.Sort?.Trim().ToLowerInvariant() switch
        {
            "price-asc" => query.OrderBy(product => product.Price).ThenBy(product => product.Name),
            "price_asc" => query.OrderBy(product => product.Price).ThenBy(product => product.Name),
            "price-desc" => query.OrderByDescending(product => product.Price).ThenBy(product => product.Name),
            "price_desc" => query.OrderByDescending(product => product.Price).ThenBy(product => product.Name),
            "name" => query.OrderBy(product => product.Name),
            "name-desc" => query.OrderByDescending(product => product.Name),
            "newest" => query.OrderByDescending(product => product.CreatedAtUtc),
            _ => query.OrderBy(product => product.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Paging.PageNumber(filter);
        var pageSize = Paging.PageSize(filter);

        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Paging.Result(
            products.Select(product => product.ToListItemDto()).ToArray(),
            filter,
            totalCount);
    }

    public async Task<ProductDetailsDto?> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(
                item => item.Slug == slug && item.IsActive && item.Category != null && item.Category.IsActive,
                cancellationToken);

        return product?.ToDetailsDto();
    }

    public async Task<IReadOnlyList<ProductListItemDto>> GetRelatedProductsAsync(
        int productId,
        int categoryId,
        int take = 8,
        CancellationToken cancellationToken = default)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product =>
                product.Id != productId &&
                product.CategoryId == categoryId &&
                product.IsActive &&
                product.Category != null &&
                product.Category.IsActive)
            .OrderBy(product => product.Name)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (products.Count < take)
        {
            var fallback = await _dbContext.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Where(product =>
                    product.Id != productId &&
                    product.CategoryId != categoryId &&
                    product.IsActive &&
                    product.Category != null &&
                    product.Category.IsActive)
                .OrderBy(product => product.Name)
                .Take(take - products.Count)
                .ToListAsync(cancellationToken);

            products.AddRange(fallback);
        }

        return products.Select(product => product.ToListItemDto()).ToArray();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(category => category.ToDto()).ToArray();
    }
}
