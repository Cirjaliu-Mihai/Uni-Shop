namespace UniShop.Api.Contracts;

public sealed record ProductListDto(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    string? ImageUrl,
    int CategoryId,
    string CategoryName,
    DateTimeOffset CreatedAt);