using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class AdminOrderService(AppDbContext dbContext) : IAdminOrderService
{
    public async Task<PagedResult<OrderListDto>> GetOrdersAsync(AdminOrderQuery query, CancellationToken cancellationToken)
    {
        var orders = dbContext.Orders.AsNoTracking();
        if (query.Status is OrderStatus status)
        {
            orders = orders.Where(order => order.Status == status);
        }
        if (query.From is DateTimeOffset from)
        {
            orders = orders.Where(order => order.CreatedAt >= from);
        }
        if (query.To is DateTimeOffset to)
        {
            orders = orders.Where(order => order.CreatedAt <= to);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            if (int.TryParse(search.TrimStart('#'), out var orderNumber))
            {
                orders = orders.Where(order => order.Id == orderNumber || EF.Functions.ILike(order.ContactEmail, $"%{EscapeLikePattern(search)}%", "\\"));
            }
            else
            {
                orders = orders.Where(order => EF.Functions.ILike(order.ContactEmail, $"%{EscapeLikePattern(search)}%", "\\"));
            }
        }

        var totalCount = await orders.CountAsync(cancellationToken);
        var page = Math.Max(query.Page ?? 1, 1);
        var pageSize = Math.Clamp(query.PageSize ?? 20, 1, 50);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var skip = (long)(page - 1) * pageSize;
        var items = skip >= totalCount
            ? []
            : await orders
                .OrderByDescending(order => order.CreatedAt)
                .ThenByDescending(order => order.Id)
                .Skip((int)skip)
                .Take(pageSize)
                .Select(order => new OrderListDto(
                    order.Id,
                    order.ContactEmail,
                    order.CreatedAt,
                    order.Status,
                    order.Items.Sum(item => item.Quantity),
                    order.Total,
                    order.PaymentMethod,
                    order.Payment.Status))
                .ToListAsync(cancellationToken);

        return new PagedResult<OrderListDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<OrderDto?> GetOrderAsync(int id, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Include(item => item.Items)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return order is null ? null : OrderService.ToDto(order, order.Payment.Status);
    }

    public async Task<OrderDto?> UpdateStatusAsync(int id, OrderStatus status, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var order = await dbContext.Orders
            .Include(item => item.Items)
            .Include(item => item.Payment)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (!IsAllowedTransition(order.Status, status))
        {
            throw new ShoppingConflictException($"Order status cannot transition from {order.Status} to {status}.");
        }

        var previousStatus = order.Status;
        var claimed = await dbContext.Orders
            .Where(item => item.Id == id && item.Status == previousStatus)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, status), cancellationToken);
        if (claimed == 0)
        {
            throw new ShoppingConflictException("The order status changed; refresh the order before trying again.");
        }

        order.Status = status;
        if (status == OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                await dbContext.Products
                    .Where(product => product.Id == item.ProductId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(product => product.Stock, product => product.Stock + item.Quantity)
                        .SetProperty(product => product.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
            }
            if (order.Payment.Status == PaymentStatus.Paid)
            {
                order.Payment.Status = PaymentStatus.Refunded;
            }
            else if (order.Payment.Status == PaymentStatus.Pending)
            {
                order.Payment.Status = PaymentStatus.Failed;
            }
        }
        else if (status == OrderStatus.Delivered && order.PaymentMethod == PaymentMethod.CashOnDelivery)
        {
            order.Payment.Status = PaymentStatus.Paid;
            order.Payment.PaidAt = DateTimeOffset.UtcNow;
        }
        else if (status == OrderStatus.Shipped && order.PaymentMethod == PaymentMethod.Card && order.Payment.Status != PaymentStatus.Paid)
        {
            throw new ShoppingConflictException("Card orders must be paid before they can be shipped.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OrderService.ToDto(order, order.Payment.Status);
    }

    private static bool IsAllowedTransition(OrderStatus current, OrderStatus next) =>
        (current, next) switch
        {
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            (OrderStatus.Processing or OrderStatus.Shipped, OrderStatus.Cancelled) => true,
            _ => false
        };

    private static string EscapeLikePattern(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}