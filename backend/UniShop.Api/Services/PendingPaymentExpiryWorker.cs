namespace UniShop.Api.Services;

public sealed class PendingPaymentExpiryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PendingPaymentExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                await paymentService.ExpirePendingOrdersAsync(DateTimeOffset.UtcNow.AddMinutes(-30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to expire pending card orders.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}