using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}