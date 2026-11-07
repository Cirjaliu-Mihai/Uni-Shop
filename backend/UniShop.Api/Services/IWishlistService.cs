using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface IWishlistService
{
    Task<IReadOnlyList<WishlistItemDto>> GetItemsAsync(string userId, CancellationToken cancellationToken);
    Task<WishlistItemDto?> AddItemAsync(string userId, int productId, CancellationToken cancellationToken);
    Task RemoveItemAsync(string userId, int productId, CancellationToken cancellationToken);
    Task<CartDto?> MoveToCartAsync(string userId, int productId, CancellationToken cancellationToken);
}