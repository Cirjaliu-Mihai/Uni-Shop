using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record CheckoutContactRequest
{
    [Required, StringLength(200)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, Phone, StringLength(40)]
    public required string Phone { get; init; }
}