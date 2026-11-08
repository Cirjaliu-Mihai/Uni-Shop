using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public enum SalesGroupBy
{
    Day,
    Week,
    Month
}

public sealed record SalesReportQuery
{
    [Required]
    public DateTimeOffset? From { get; init; }

    [Required]
    public DateTimeOffset? To { get; init; }

    public SalesGroupBy GroupBy { get; init; } = SalesGroupBy.Day;
}