using UniShop.Api.Domain;

namespace UniShop.Api.Contracts;

public sealed record AdminOrderQuery
{
    public OrderStatus? Status { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public string? Search { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}