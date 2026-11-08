using UniShop.Api.Contracts;
using UniShop.Api.Domain;

namespace UniShop.Api.Services;

public sealed class MockPaymentGateway : IPaymentGateway
{
    public Task<PaymentGatewayResult> ProcessAsync(PaymentGatewayRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var paid = request.Method == PaymentMethod.Card;
        var result = new PaymentGatewayResult(
            paid ? PaymentStatus.Paid : PaymentStatus.Pending,
            $"mock-{request.Method.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}",
            paid ? DateTimeOffset.UtcNow : null);
        return Task.FromResult(result);
    }
}