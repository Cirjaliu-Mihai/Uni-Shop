using Stripe;
using Stripe.Checkout;
using UniShop.Api.Domain;

namespace UniShop.Api.Services;

public sealed class StripePaymentGateway(IConfiguration configuration) : IPaymentGateway
{
    public async Task<StripeCheckoutSession> CreateCheckoutSessionAsync(
        Order order,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken)
    {
        var secretKey = configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new PaymentProviderUnavailableException("Stripe is not configured. Set Stripe:SecretKey before accepting card payments.");
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = order.ContactEmail,
            ClientReferenceId = order.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = order.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            LineItems = order.Items.Select(item => new SessionLineItemOptions
            {
                Quantity = item.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "ron",
                    UnitAmount = ToMinorUnits(item.UnitPrice),
                    ProductData = new SessionLineItemPriceDataProductDataOptions { Name = item.ProductName }
                }
            }).ToList()
        };

        if (order.DeliveryCost > 0)
        {
            options.LineItems.Add(new SessionLineItemOptions
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "ron",
                    UnitAmount = ToMinorUnits(order.DeliveryCost),
                    ProductData = new SessionLineItemPriceDataProductDataOptions { Name = order.DeliveryMethodName }
                }
            });
        }

        var stripeClient = new StripeClient(secretKey);
        var previousSession = string.IsNullOrWhiteSpace(order.Payment.ProviderReference)
            ? "initial"
            : order.Payment.ProviderReference;
        var session = await stripeClient.V1.Checkout.Sessions.CreateAsync(
            options,
            new RequestOptions { IdempotencyKey = $"unishop-order-{order.Id}-checkout-{previousSession}" },
            cancellationToken);
        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new PaymentProviderUnavailableException("Stripe did not return a Checkout URL.");
        }

        return new StripeCheckoutSession(session.Id, session.Url);
    }

    public VerifiedStripeEvent VerifyWebhook(string payload, string signature)
    {
        var webhookSecret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new PaymentProviderUnavailableException("Stripe webhook verification is not configured. Set Stripe:WebhookSecret.");
        }

        var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret, EventUtility.DefaultTimeTolerance, false);
        var session = stripeEvent.Data.Object as Session;
        var orderId = session?.ClientReferenceId;
        if (string.IsNullOrWhiteSpace(orderId) && session?.Metadata is not null)
        {
            session.Metadata.TryGetValue("orderId", out orderId);
        }

        return new VerifiedStripeEvent(
            stripeEvent.Id,
            stripeEvent.Type,
            orderId,
            session?.Id,
            session?.PaymentIntentId,
            session?.PaymentStatus,
            session?.AmountTotal,
            session?.Currency);
    }

    private static long ToMinorUnits(decimal amount) =>
        decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
}