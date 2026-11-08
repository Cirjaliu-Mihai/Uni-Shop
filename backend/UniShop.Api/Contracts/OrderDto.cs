using UniShop.Api.Domain;

namespace UniShop.Api.Contracts;

public sealed record OrderDto(
    int Id,
    DateTimeOffset CreatedAt,
    OrderStatus Status,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string ShippingAddressLabel,
    string ShippingFullName,
    string ShippingPhone,
    string ShippingStreet,
    string ShippingCity,
    string ShippingCounty,
    string ShippingPostalCode,
    string ShippingCountry,
    string DeliveryMethodName,
    decimal DeliveryMethodPrice,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    decimal Subtotal,
    decimal DeliveryCost,
    decimal Total,
    string? Notes,
    IReadOnlyList<OrderItemDto> Items);