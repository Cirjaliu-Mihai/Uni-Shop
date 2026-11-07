using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record MergeGuestCartRequest
{
    [Required]
    public List<GuestCartItemRequest> Items { get; init; } = [];
}