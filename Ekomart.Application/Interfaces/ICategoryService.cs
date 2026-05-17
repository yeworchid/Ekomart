using Ekomart.Application.DTOs.Admin;
using Ekomart.Application.DTOs.Catalog;

namespace Ekomart.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<CategoryEditDto?> GetCategoryForEditAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<int> CreateCategoryAsync(
        CategoryEditDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task UpdateCategoryAsync(
        CategoryEditDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(
        int id,
        string adminUserId,
        CancellationToken cancellationToken = default);
}
