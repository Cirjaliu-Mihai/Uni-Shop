namespace UniShop.Api.Contracts;

public sealed record ProductQuery
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? InStock { get; init; }
    public string? Sort { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}