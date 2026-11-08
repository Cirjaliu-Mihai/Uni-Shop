using UniShop.Api.Domain;

namespace UniShop.Api.Contracts;

public sealed record OrderHistoryQuery
{
    public OrderStatus? Status { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}