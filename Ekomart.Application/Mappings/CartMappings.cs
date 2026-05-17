using Ekomart.Application.DTOs.Cart;
using Ekomart.Domain.Entities;

namespace Ekomart.Application.Mappings;

public static class CartMappings
{
    public static CartItemDto ToDto(this CartItem cartItem)
    {
        var unitPrice = cartItem.Product?.Price ?? 0;

        return new CartItemDto
        {
            ProductId = cartItem.ProductId,
            ProductName = cartItem.Product?.Name ?? string.Empty,
            ProductSlug = cartItem.Product?.Slug ?? string.Empty,
            ImageUrl = cartItem.Product?.ImageUrl,
            UnitPrice = unitPrice,
            Quantity = cartItem.Quantity,
            LineTotal = unitPrice * cartItem.Quantity
        };
    }
}
