using UniShop.Api.Domain;

namespace UniShop.Api.Services;

public interface IPaymentGateway
{
    Task<StripeCheckoutSession> CreateCheckoutSessionAsync(
        Order order,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken);

    VerifiedStripeEvent VerifyWebhook(string payload, string signature);
}

public sealed record StripeCheckoutSession(string Id, string Url);

public sealed record VerifiedStripeEvent(
    string Id,
    string Type,
    string? OrderId,
    string? SessionId,
    string? PaymentIntentId,
    string? PaymentStatus,
    long? AmountTotal,
    string? Currency);