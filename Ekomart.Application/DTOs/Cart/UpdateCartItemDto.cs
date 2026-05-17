using System.ComponentModel.DataAnnotations;

namespace Ekomart.Application.DTOs.Cart;

public class UpdateCartItemDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 99)]
    public int Quantity { get; set; }
}
