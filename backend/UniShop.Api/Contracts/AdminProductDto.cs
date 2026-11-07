namespace UniShop.Api.Contracts;

public sealed record AdminProductDto(
    int Id,
    string Name,
    string Description,
    IReadOnlyList<ProductSpecificationDto> Specifications,
    decimal Price,
    int Stock,
    string? ImageUrl,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int CategoryId,
    string CategoryName,
    int SupplierId,
    string SupplierName,
    IReadOnlyList<int> DeliveryMethodIds);