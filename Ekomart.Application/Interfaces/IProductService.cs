using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Admin;

namespace Ekomart.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductAdminListItemDto>> GetProductsForAdminAsync(
        AdminProductFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<ProductEditDto?> GetProductForEditAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<int> CreateProductAsync(
        ProductEditDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task UpdateProductAsync(
        ProductEditDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task DeleteProductAsync(
        int id,
        string adminUserId,
        CancellationToken cancellationToken = default);
}
