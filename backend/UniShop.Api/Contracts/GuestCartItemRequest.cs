using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record GuestCartItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}