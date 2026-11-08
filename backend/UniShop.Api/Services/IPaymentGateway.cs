using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ProcessAsync(PaymentGatewayRequest request, CancellationToken cancellationToken);
}