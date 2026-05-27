using Ekomart.Application.DTOs.Catalog;

namespace Ekomart.Web.Models.Store;

public class ProductCardViewModel
{
    public ProductListItemDto Product { get; set; } = new();
    public string ReturnUrl { get; set; } = "/";
    public string AddButtonText { get; set; } = "Add To Cart";
}
