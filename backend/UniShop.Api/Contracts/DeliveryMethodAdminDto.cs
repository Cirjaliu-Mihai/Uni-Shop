namespace UniShop.Api.Contracts;

public sealed record DeliveryMethodAdminDto(
    int Id,
    string Name,
    decimal Price,
    int EstimatedDays,
    int ProductCount);