using Ekomart.Application.DTOs.Catalog;

namespace Ekomart.Web.Models.Store;

public class HomeIndexViewModel
{
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public IReadOnlyList<ProductListItemDto> FeaturedProducts { get; set; } = Array.Empty<ProductListItemDto>();
}
