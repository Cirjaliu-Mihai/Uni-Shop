namespace UniShop.Api.Contracts;

public sealed record AdminProductQuery
{
    public string? Search { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}