using Ekomart.Application.DTOs.Cart;
using Ekomart.Application.Interfaces;
using Ekomart.Application.Mappings;
using Ekomart.Domain.Entities;
using Ekomart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ekomart.Infrastructure.Services;

public class CartService : ICartService
{
    private readonly AppDbContext _dbContext;

    public CartService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CartDto> GetCartAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = await GetUserCartItems(userId)
            .ToListAsync(cancellationToken);

        return BuildCart(items);
    }

    public async Task<CartDto> AddItemAsync(
        string userId,
        int productId,
        int quantity = 1,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        var product = await _dbContext.Products
            .FirstOrDefaultAsync(item => item.Id == productId && item.IsActive, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("Product was not found.");
        }

        if (product.StockQuantity <= 0)
        {
            throw new InvalidOperationException("Product is out of stock.");
        }

        var cartItem = await _dbContext.CartItems
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.ProductId == productId,
                cancellationToken);

        if (cartItem is null)
        {
            cartItem = new CartItem
            {
                UserId = userId,
                ProductId = productId,
                Quantity = Math.Min(quantity, product.StockQuantity)
            };

            _dbContext.CartItems.Add(cartItem);
        }
        else
        {
            cartItem.Quantity = Math.Min(cartItem.Quantity + quantity, product.StockQuantity);
            cartItem.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetCartAsync(userId, cancellationToken);
    }

    public async Task<CartDto> UpdateQuantityAsync(
        string userId,
        UpdateCartItemDto dto,
        CancellationToken cancellationToken = default)
    {
        var cartItem = await _dbContext.CartItems
            .Include(item => item.Product)
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.ProductId == dto.ProductId,
                cancellationToken);

        if (cartItem is null)
        {
            throw new InvalidOperationException("Cart item was not found.");
        }

        var stockQuantity = cartItem.Product?.StockQuantity ?? 0;
        if (stockQuantity <= 0)
        {
            _dbContext.CartItems.Remove(cartItem);
        }
        else
        {
            cartItem.Quantity = Math.Min(dto.Quantity, stockQuantity);
            cartItem.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetCartAsync(userId, cancellationToken);
    }

    public async Task<CartDto> RemoveItemAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var cartItem = await _dbContext.CartItems
            .FirstOrDefaultAsync(
                item => item.UserId == userId && item.ProductId == productId,
                cancellationToken);

        if (cartItem is not null)
        {
            _dbContext.CartItems.Remove(cartItem);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetCartAsync(userId, cancellationToken);
    }

    public async Task<int> GetTotalQuantityAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CartItems
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .SumAsync(item => item.Quantity, cancellationToken);
    }

    public async Task ClearAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cartItems = await _dbContext.CartItems
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);

        _dbContext.CartItems.RemoveRange(cartItems);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<CartItem> GetUserCartItems(string userId)
    {
        return _dbContext.CartItems
            .AsNoTracking()
            .Include(item => item.Product)
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.CreatedAtUtc);
    }

    private static CartDto BuildCart(IReadOnlyList<CartItem> cartItems)
    {
        var items = cartItems
            .Where(item => item.Product is not null)
            .Select(item => item.ToDto())
            .ToArray();

        return new CartDto
        {
            Items = items,
            ItemsTotal = items.Sum(item => item.LineTotal),
            TotalQuantity = items.Sum(item => item.Quantity)
        };
    }
}
