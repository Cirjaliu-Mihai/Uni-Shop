namespace UniShop.Api.Contracts;

public sealed record SupplierAdminDto(
    int Id,
    string Name,
    string ContactEmail,
    string Phone,
    int ProductCount);