using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class AdminReportService(AppDbContext dbContext) : IAdminReportService
{
    public async Task<IReadOnlyList<SalesReportPointDto>> GetSalesAsync(SalesReportQuery query, CancellationToken cancellationToken)
    {
        var from = query.From!.Value;
        var to = query.To!.Value;
        var orders = await dbContext.Orders.AsNoTracking()
            .Where(order => order.Status != OrderStatus.Cancelled && order.CreatedAt >= from && order.CreatedAt <= to)
            .Select(order => new { order.CreatedAt, order.Total })
            .ToListAsync(cancellationToken);

        return orders
            .GroupBy(order => StartOfPeriod(order.CreatedAt, query.GroupBy))
            .OrderBy(group => group.Key)
            .Select(group => new SalesReportPointDto(group.Key, group.Count(), group.Sum(order => order.Total)))
            .ToArray();
    }

    public async Task<IReadOnlyList<PopularProductReportDto>> GetPopularProductsAsync(
        PopularProductsReportQuery query,
        CancellationToken cancellationToken)
    {
        var from = query.From!.Value;
        var to = query.To!.Value;
        var items = await dbContext.OrderItems.AsNoTracking()
            .Where(item => item.Order.Status != OrderStatus.Cancelled &&
                item.Order.CreatedAt >= from && item.Order.CreatedAt <= to)
            .Select(item => new { item.ProductId, item.ProductName, item.Quantity, item.LineTotal })
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(item => new { item.ProductId, item.ProductName })
            .Select(group => new PopularProductReportDto(
                group.Key.ProductId,
                group.Key.ProductName,
                group.Sum(item => item.Quantity),
                group.Sum(item => item.LineTotal)))
            .OrderByDescending(item => item.QuantitySold)
            .ThenByDescending(item => item.Revenue)
            .Take(Math.Clamp(query.Top, 1, 50))
            .ToArray();
    }

    private static DateTimeOffset StartOfPeriod(DateTimeOffset value, SalesGroupBy groupBy)
    {
        var utc = value.UtcDateTime;
        var date = groupBy switch
        {
            SalesGroupBy.Week => utc.Date.AddDays(-(((int)utc.DayOfWeek + 6) % 7)),
            SalesGroupBy.Month => new DateTime(utc.Year, utc.Month, 1),
            _ => utc.Date
        };
        return new DateTimeOffset(date, TimeSpan.Zero);
    }
}