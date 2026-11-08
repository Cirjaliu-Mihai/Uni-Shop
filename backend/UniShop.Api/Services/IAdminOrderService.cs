using UniShop.Api.Contracts;
using UniShop.Api.Domain;

namespace UniShop.Api.Services;

public interface IAdminOrderService
{
    Task<PagedResult<OrderListDto>> GetOrdersAsync(AdminOrderQuery query, CancellationToken cancellationToken);
    Task<OrderDto?> GetOrderAsync(int id, CancellationToken cancellationToken);
    Task<OrderDto?> UpdateStatusAsync(int id, OrderStatus status, CancellationToken cancellationToken);
}