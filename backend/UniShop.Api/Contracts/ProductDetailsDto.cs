namespace UniShop.Api.Contracts;

public sealed record ProductDetailsDto(
    int Id,
    string Name,
    string Description,
    Dictionary<string, string> Specifications,
    decimal Price,
    int Stock,
    string? ImageUrl,
    int CategoryId,
    string CategoryName,
    string SupplierName,
    IReadOnlyList<DeliveryMethodDto> DeliveryMethods);