using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId, CancellationToken cancellationToken);
    Task<CartDto?> SetQuantityAsync(string userId, int productId, int quantity, CancellationToken cancellationToken);
    Task<bool> RemoveItemAsync(string userId, int productId, CancellationToken cancellationToken);
    Task<CartDto> ClearCartAsync(string userId, CancellationToken cancellationToken);
    Task<CartDto> MergeGuestCartAsync(string userId, IReadOnlyList<GuestCartItemRequest> items, CancellationToken cancellationToken);
}