using Ekomart.Application.DTOs.Catalog;

namespace Ekomart.Web.Models.Store;

public class ProductDetailsViewModel
{
    public ProductDetailsDto Product { get; set; } = new();
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public IReadOnlyList<ProductListItemDto> RelatedProducts { get; set; } = Array.Empty<ProductListItemDto>();
}
