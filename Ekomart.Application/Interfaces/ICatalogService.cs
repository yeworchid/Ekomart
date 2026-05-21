using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Catalog;

namespace Ekomart.Application.Interfaces;

public interface ICatalogService
{
    Task<PagedResult<ProductListItemDto>> GetProductsAsync(
        CatalogFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<ProductDetailsDto?> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductListItemDto>> GetRelatedProductsAsync(
        int productId,
        int categoryId,
        int take = 8,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(
        CancellationToken cancellationToken = default);
}
