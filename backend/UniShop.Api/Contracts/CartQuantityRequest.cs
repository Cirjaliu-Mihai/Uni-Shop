using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record CartQuantityRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}