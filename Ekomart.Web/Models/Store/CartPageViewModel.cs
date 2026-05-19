using Ekomart.Application.DTOs.Cart;

namespace Ekomart.Web.Models.Store;

public class CartPageViewModel
{
    public CartDto Cart { get; set; } = new();
}
