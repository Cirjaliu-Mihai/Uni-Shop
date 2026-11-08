using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface IOrderService
{
    Task<IReadOnlyList<DeliveryOptionDto>> GetDeliveryOptionsAsync(string userId, CancellationToken cancellationToken);
    Task<OrderDto?> CreateOrderAsync(string userId, CreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderDto?> GetOrderAsync(string userId, int orderId, CancellationToken cancellationToken);
    Task<PagedResult<OrderListDto>> GetOrdersAsync(string userId, OrderHistoryQuery query, CancellationToken cancellationToken);
    Task<OrderDto?> CancelOrderAsync(string userId, int orderId, CancellationToken cancellationToken);
}