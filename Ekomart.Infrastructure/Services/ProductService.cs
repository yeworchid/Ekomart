using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Admin;
using Ekomart.Application.Interfaces;
using Ekomart.Application.Mappings;
using Ekomart.Domain.Entities;
using Ekomart.Domain.Enums;
using Ekomart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ekomart.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _dbContext;

    public ProductService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProductAdminListItemDto>> GetProductsForAdminAsync(
        AdminProductFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = $"%{filter.Search.Trim()}%";
            query = query.Where(product =>
                EF.Functions.ILike(product.Name, search) ||
                EF.Functions.ILike(product.Slug, search));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == filter.CategoryId.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(product => product.IsActive == filter.IsActive.Value);
        }

        query = query.OrderBy(product => product.Name);

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Paging.PageNumber(filter);
        var pageSize = Paging.PageSize(filter);

        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Paging.Result(
            products.Select(product => product.ToAdminListItemDto()).ToArray(),
            filter,
            totalCount);
    }

    public async Task<ProductEditDto?> GetProductForEditAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return product?.ToEditDto();
    }

    public async Task<int> CreateProductAsync(
        ProductEditDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var product = new Product();
        dto.ApplyTo(product);
        product.CreatedAtUtc = DateTime.UtcNow;
        product.UpdatedAtUtc = null;

        _dbContext.Products.Add(product);
        AddAudit(adminUserId, AuditAction.ProductCreated, product, $"Product created: {product.Name}.");
        await _dbContext.SaveChangesAsync(cancellationToken);

        return product.Id;
    }

    public async Task UpdateProductAsync(
        ProductEditDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(item => item.Id == dto.Id, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("Product was not found.");
        }

        dto.ApplyTo(product);
        AddAudit(adminUserId, AuditAction.ProductUpdated, product, $"Product updated: {product.Name}.");
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteProductAsync(
        int id,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return;
        }

        product.IsActive = false;
        product.UpdatedAtUtc = DateTime.UtcNow;

        AddAudit(adminUserId, AuditAction.ProductDeleted, product, $"Product disabled: {product.Name}.");
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AddAudit(
        string userId,
        AuditAction action,
        Product product,
        string details)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = nameof(Product),
            EntityId = product.Id == 0 ? null : product.Id.ToString(),
            Details = details
        });
    }
}
