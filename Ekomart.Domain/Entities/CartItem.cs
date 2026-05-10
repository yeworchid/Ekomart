using Ekomart.Domain.Common;

namespace Ekomart.Domain.Entities;

public class CartItem : Entity
{
    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
}