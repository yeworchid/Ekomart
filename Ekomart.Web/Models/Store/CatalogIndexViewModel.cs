using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Catalog;

namespace Ekomart.Web.Models.Store;

public class CatalogIndexViewModel
{
    public CatalogFilterDto Filter { get; set; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public PagedResult<ProductListItemDto> Products { get; set; } = new();
}
