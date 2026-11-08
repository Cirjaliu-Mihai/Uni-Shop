using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record PopularProductsReportQuery
{
    [Required]
    public DateTimeOffset? From { get; init; }

    [Required]
    public DateTimeOffset? To { get; init; }

    [Range(1, 50)]
    public int Top { get; init; } = 10;
}