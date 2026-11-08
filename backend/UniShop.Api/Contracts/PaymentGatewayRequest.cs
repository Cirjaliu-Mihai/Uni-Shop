using UniShop.Api.Domain;

namespace UniShop.Api.Contracts;

public sealed record PaymentGatewayRequest(int OrderId, decimal Amount, string Currency, PaymentMethod Method);