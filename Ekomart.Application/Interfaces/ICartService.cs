using Ekomart.Application.DTOs.Cart;

namespace Ekomart.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId, CancellationToken cancellationToken = default);

    Task<CartDto> AddItemAsync(
        string userId,
        int productId,
        int quantity = 1,
        CancellationToken cancellationToken = default);

    Task<CartDto> UpdateQuantityAsync(
        string userId,
        UpdateCartItemDto dto,
        CancellationToken cancellationToken = default);

    Task<CartDto> RemoveItemAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalQuantityAsync(string userId, CancellationToken cancellationToken = default);

    Task ClearAsync(string userId, CancellationToken cancellationToken = default);
}
