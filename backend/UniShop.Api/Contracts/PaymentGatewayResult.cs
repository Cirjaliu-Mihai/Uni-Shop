using UniShop.Api.Domain;

namespace UniShop.Api.Contracts;

public sealed record PaymentGatewayResult(PaymentStatus Status, string? ProviderReference, DateTimeOffset? PaidAt);