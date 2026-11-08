using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record AdminUserQuery
{
    [StringLength(200)]
    public string? Search { get; init; }

    [Range(1, int.MaxValue)]
    public int? Page { get; init; }

    [Range(1, 50)]
    public int? PageSize { get; init; }
}