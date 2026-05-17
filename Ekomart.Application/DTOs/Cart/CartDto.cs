namespace Ekomart.Application.DTOs.Cart;

public class CartDto
{
    public IReadOnlyList<CartItemDto> Items { get; set; } = Array.Empty<CartItemDto>();
    public decimal ItemsTotal { get; set; }
    public int TotalQuantity { get; set; }
}
