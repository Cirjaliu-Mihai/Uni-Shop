using UniShop.Api.Domain;

namespace UniShop.Api.Contracts;

public sealed record OrderListDto(
    int Id,
    string ContactEmail,
    DateTimeOffset CreatedAt,
    OrderStatus Status,
    int ItemCount,
    decimal Total,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus);