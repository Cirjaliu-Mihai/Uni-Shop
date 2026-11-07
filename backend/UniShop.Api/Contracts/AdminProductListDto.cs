namespace UniShop.Api.Contracts;

public sealed record AdminProductListDto(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    bool IsActive,
    string CategoryName,
    string SupplierName,
    DateTimeOffset UpdatedAt);