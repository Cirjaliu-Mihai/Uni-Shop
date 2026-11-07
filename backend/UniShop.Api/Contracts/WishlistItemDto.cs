namespace UniShop.Api.Contracts;

public sealed record WishlistItemDto(
    int ProductId,
    string Name,
    decimal Price,
    string? ImageUrl,
    int Stock,
    int CategoryId,
    string CategoryName,
    DateTimeOffset AddedAt);