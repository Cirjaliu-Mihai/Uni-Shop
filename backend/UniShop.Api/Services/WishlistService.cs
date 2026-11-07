using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class WishlistService(AppDbContext dbContext) : IWishlistService
{
    public async Task<IReadOnlyList<WishlistItemDto>> GetItemsAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.WishlistItems
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Product.IsActive)
            .OrderByDescending(item => item.AddedAt)
            .Select(item => new WishlistItemDto(
                item.ProductId,
                item.Product.Name,
                item.Product.Price,
                item.Product.ImageUrl,
                item.Product.Stock,
                item.Product.CategoryId,
                item.Product.Category.Name,
                item.AddedAt))
            .ToListAsync(cancellationToken);

    public async Task<WishlistItemDto?> AddItemAsync(string userId, int productId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == productId && item.IsActive)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Price,
                item.ImageUrl,
                item.Stock,
                item.CategoryId,
                CategoryName = item.Category.Name
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null)
        {
            return null;
        }

        var existingItem = await dbContext.WishlistItems.FindAsync([userId, productId], cancellationToken);
        if (existingItem is not null)
        {
            return new WishlistItemDto(
                product.Id,
                product.Name,
                product.Price,
                product.ImageUrl,
                product.Stock,
                product.CategoryId,
                product.CategoryName,
                existingItem.AddedAt);
        }

        var wishlistItem = new WishlistItem { UserId = userId, ProductId = productId };
        dbContext.WishlistItems.Add(wishlistItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new WishlistItemDto(
            product.Id,
            product.Name,
            product.Price,
            product.ImageUrl,
            product.Stock,
            product.CategoryId,
            product.CategoryName,
            wishlistItem.AddedAt);
    }

    public async Task RemoveItemAsync(string userId, int productId, CancellationToken cancellationToken)
    {
        var item = await dbContext.WishlistItems.FindAsync([userId, productId], cancellationToken);
        if (item is null)
        {
            return;
        }

        dbContext.WishlistItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CartDto?> MoveToCartAsync(string userId, int productId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var wishlistItem = await dbContext.WishlistItems
            .Include(item => item.Product)
            .SingleOrDefaultAsync(item => item.UserId == userId && item.ProductId == productId, cancellationToken);
        if (wishlistItem is null || !wishlistItem.Product.IsActive)
        {
            return null;
        }
        if (wishlistItem.Product.Stock < 1)
        {
            throw new ShoppingConflictException("This product is out of stock and cannot be moved to the cart.");
        }

        var cart = await CartService.GetOrCreateCartAsync(dbContext, userId, cancellationToken);
        var cartItem = cart.Items.SingleOrDefault(item => item.ProductId == productId);
        if (cartItem is null)
        {
            cart.Items.Add(new CartItem { ProductId = productId, Quantity = 1 });
        }
        else if (cartItem.Quantity >= wishlistItem.Product.Stock)
        {
            throw new ShoppingConflictException("The cart quantity already meets the available stock.");
        }
        else
        {
            cartItem.Quantity++;
        }

        cart.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.WishlistItems.Remove(wishlistItem);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CartService.ToDto(cart);
    }
}