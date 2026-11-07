namespace UniShop.Api.Contracts;

public sealed record CartItemDto(
    int ProductId,
    string Name,
    string? ImageUrl,
    decimal Price,
    int Stock,
    int Quantity,
    decimal LineTotal,
    bool ExceedsStock);