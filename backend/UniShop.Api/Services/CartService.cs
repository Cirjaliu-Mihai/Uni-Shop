using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class CartService(AppDbContext dbContext) : ICartService
{
    public async Task<CartDto> GetCartAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await GetOrCreateCartAsync(dbContext, userId, cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto?> SetQuantityAsync(
        string userId,
        int productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == productId && item.IsActive, cancellationToken);
        if (product is null)
        {
            return null;
        }
        if (quantity > product.Stock)
        {
            throw new ShoppingConflictException($"Only {product.Stock} units of this product are available.");
        }

        var cart = await GetOrCreateCartAsync(dbContext, userId, cancellationToken);
        var cartItem = cart.Items.SingleOrDefault(item => item.ProductId == productId);
        if (cartItem is null)
        {
            cart.Items.Add(new CartItem { ProductId = productId, Quantity = quantity });
        }
        else
        {
            cartItem.Quantity = quantity;
        }

        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(cart);
    }

    public async Task<bool> RemoveItemAsync(string userId, int productId, CancellationToken cancellationToken)
    {
        var cart = await dbContext.Carts
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var cartItem = cart?.Items.SingleOrDefault(item => item.ProductId == productId);
        if (cart is null || cartItem is null)
        {
            return false;
        }

        cart.Items.Remove(cartItem);
        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CartDto> ClearCartAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await GetOrCreateCartAsync(dbContext, userId, cancellationToken);
        dbContext.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(cart);
    }

    public async Task<CartDto> MergeGuestCartAsync(
        string userId,
        IReadOnlyList<GuestCartItemRequest> items,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var cart = await GetOrCreateCartAsync(dbContext, userId, cancellationToken);
        var mergedItems = items
            .GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => (long)item.Quantity) })
            .ToArray();
        var productIds = mergedItems.Select(item => item.ProductId).ToArray();
        var products = await dbContext.Products
            .Where(product => product.IsActive && productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        foreach (var mergedItem in mergedItems)
        {
            if (!products.TryGetValue(mergedItem.ProductId, out var product))
            {
                continue;
            }

            var cartItem = cart.Items.SingleOrDefault(item => item.ProductId == mergedItem.ProductId);
            var quantity = (int)Math.Min((long)(cartItem?.Quantity ?? 0) + mergedItem.Quantity, product.Stock);
            if (quantity < 1)
            {
                if (cartItem is not null)
                {
                    cart.Items.Remove(cartItem);
                }
                continue;
            }

            if (cartItem is null)
            {
                cart.Items.Add(new CartItem { ProductId = product.Id, Quantity = quantity });
            }
            else
            {
                cartItem.Quantity = quantity;
            }
        }

        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(cart);
    }

    internal static async Task<Cart> GetOrCreateCartAsync(
        AppDbContext dbContext,
        string userId,
        CancellationToken cancellationToken)
    {
        var cart = await dbContext.Carts
            .Include(item => item.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart { UserId = userId };
        dbContext.Carts.Add(cart);
        await dbContext.SaveChangesAsync(cancellationToken);
        return cart;
    }

    internal static CartDto ToDto(Cart cart)
    {
        var items = cart.Items
            .Where(item => item.Product is not null)
            .OrderBy(item => item.Product.Name)
            .Select(item => new CartItemDto(
                item.ProductId,
                item.Product.Name,
                item.Product.ImageUrl,
                item.Product.Price,
                item.Product.Stock,
                item.Quantity,
                item.Product.Price * item.Quantity,
                item.Quantity > item.Product.Stock))
            .ToArray();

        return new CartDto(items, items.Sum(item => item.LineTotal), items.Any(item => item.ExceedsStock));
    }
}