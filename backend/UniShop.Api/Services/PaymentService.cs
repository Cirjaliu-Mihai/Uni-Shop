using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class PaymentService(
    AppDbContext dbContext,
    IPaymentGateway paymentGateway,
    IConfiguration configuration) : IPaymentService
{
    public async Task<CheckoutSessionDto?> CreateCheckoutSessionAsync(
        string userId,
        int orderId,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Include(item => item.Items)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item => item.Id == orderId && item.UserId == userId, cancellationToken);
        if (order is null)
        {
            return null;
        }
        if (order.PaymentMethod != PaymentMethod.Card || order.Payment.Status != PaymentStatus.Pending || order.Status != OrderStatus.Processing)
        {
            throw new ShoppingConflictException("Only processing card orders with a pending payment can be paid.");
        }
        if (order.Payment.CreatedAt <= DateTimeOffset.UtcNow.AddMinutes(-30))
        {
            throw new ShoppingConflictException("This payment session has expired. The order will be cancelled and its stock released.");
        }

        var baseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var frontendUri) ||
            (frontendUri.Scheme != Uri.UriSchemeHttp && frontendUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Frontend:BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        var orderUrl = new Uri(frontendUri, $"/orders/confirmation/{order.Id}").ToString();
        var checkout = await paymentGateway.CreateCheckoutSessionAsync(
            order,
            $"{orderUrl}?payment=success&session_id={{CHECKOUT_SESSION_ID}}",
            $"{orderUrl}?payment=cancelled",
            cancellationToken);

        var updated = await dbContext.Payments
            .Where(payment => payment.OrderId == order.Id && payment.Method == PaymentMethod.Card && payment.Status == PaymentStatus.Pending && payment.Order.Status == OrderStatus.Processing)
            .ExecuteUpdateAsync(setters => setters.SetProperty(payment => payment.ProviderReference, checkout.Id), cancellationToken);
        if (updated == 0)
        {
            throw new ShoppingConflictException("The order is no longer available for payment.");
        }

        return new CheckoutSessionDto(checkout.Url);
    }

    public async Task<bool> HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken)
    {
        var stripeEvent = paymentGateway.VerifyWebhook(payload, signature);
        if (stripeEvent.Type != "checkout.session.completed" ||
            !string.Equals(stripeEvent.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(stripeEvent.OrderId, out var orderId) ||
            string.IsNullOrWhiteSpace(stripeEvent.SessionId))
        {
            return false;
        }

        var payment = await dbContext.Payments.AsNoTracking()
            .Where(item => item.OrderId == orderId && item.Method == PaymentMethod.Card)
            .Select(item => new { item.Amount, item.Currency })
            .SingleOrDefaultAsync(cancellationToken);
        if (payment is null ||
            stripeEvent.AmountTotal != ToMinorUnits(payment.Amount) ||
            !string.Equals(stripeEvent.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var reference = stripeEvent.PaymentIntentId ?? stripeEvent.SessionId;
        var updated = await dbContext.Payments
            .Where(item => item.OrderId == orderId &&
                item.Method == PaymentMethod.Card &&
                item.Status == PaymentStatus.Pending &&
                item.ProviderReference == stripeEvent.SessionId &&
                item.Order.Status == OrderStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, PaymentStatus.Paid)
                .SetProperty(item => item.PaidAt, DateTimeOffset.UtcNow)
                .SetProperty(item => item.ProviderReference, reference), cancellationToken);

        return updated > 0;
    }

    public async Task<int> ExpirePendingOrdersAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken)
    {
        var orderIds = await dbContext.Payments.AsNoTracking()
            .Where(payment => payment.Method == PaymentMethod.Card &&
                payment.Status == PaymentStatus.Pending &&
                payment.CreatedAt <= expiredBefore &&
                payment.Order.Status == OrderStatus.Processing)
            .Select(payment => payment.OrderId)
            .ToListAsync(cancellationToken);

        var expiredCount = 0;
        foreach (var orderId in orderIds)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var paymentUpdated = await dbContext.Payments
                .Where(payment => payment.OrderId == orderId &&
                    payment.Method == PaymentMethod.Card &&
                    payment.Status == PaymentStatus.Pending &&
                    payment.CreatedAt <= expiredBefore &&
                    payment.Order.Status == OrderStatus.Processing)
                .ExecuteUpdateAsync(setters => setters.SetProperty(payment => payment.Status, PaymentStatus.Failed), cancellationToken);
            if (paymentUpdated == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                continue;
            }

            var order = await dbContext.Orders.AsNoTracking()
                .Include(item => item.Items)
                .SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken);
            var orderUpdated = await dbContext.Orders
                .Where(item => item.Id == orderId && item.Status == OrderStatus.Processing)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, OrderStatus.Cancelled), cancellationToken);
            if (order is null || orderUpdated == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                continue;
            }

            foreach (var item in order.Items)
            {
                await dbContext.Products
                    .Where(product => product.Id == item.ProductId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(product => product.Stock, product => product.Stock + item.Quantity)
                        .SetProperty(product => product.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            expiredCount++;
        }

        return expiredCount;
    }

    private static long ToMinorUnits(decimal amount) =>
        decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
}