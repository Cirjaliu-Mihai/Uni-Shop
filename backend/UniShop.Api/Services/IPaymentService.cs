using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface IPaymentService
{
    Task<CheckoutSessionDto?> CreateCheckoutSessionAsync(string userId, int orderId, CancellationToken cancellationToken);
    Task<bool> HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken);
    Task<int> ExpirePendingOrdersAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken);
}