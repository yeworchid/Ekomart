using Ekomart.Application.DTOs.Admin;
using Ekomart.Domain.Entities;

namespace Ekomart.Application.Mappings;

public static class AdminMappings
{
    public static ProductAdminListItemDto ToAdminListItemDto(this Product product)
    {
        return new ProductAdminListItemDto
        {
            Id = product.Id,
            CategoryName = product.Category?.Name ?? string.Empty,
            Name = product.Name,
            Slug = product.Slug,
            ImageUrl = product.ImageUrl,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            IsActive = product.IsActive,
            CreatedAtUtc = product.CreatedAtUtc
        };
    }

    public static ProductEditDto ToEditDto(this Product product)
    {
        return new ProductEditDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            StockQuantity = product.StockQuantity,
            IsActive = product.IsActive
        };
    }

    public static CategoryEditDto ToEditDto(this Category category)
    {
        return new CategoryEditDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        };
    }

    public static void ApplyTo(this ProductEditDto dto, Product product)
    {
        product.CategoryId = dto.CategoryId;
        product.Name = dto.Name;
        product.Slug = dto.Slug;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.ImageUrl = dto.ImageUrl;
        product.StockQuantity = dto.StockQuantity;
        product.IsActive = dto.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static void ApplyTo(this CategoryEditDto dto, Category category)
    {
        category.Name = dto.Name;
        category.Slug = dto.Slug;
        category.Description = dto.Description;
        category.DisplayOrder = dto.DisplayOrder;
        category.IsActive = dto.IsActive;
        category.UpdatedAtUtc = DateTime.UtcNow;
    }
}
