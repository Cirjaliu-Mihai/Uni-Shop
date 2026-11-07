using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record RegisterRequest
{
    [Required, StringLength(200)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, MinLength(8), RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$")]
    public required string Password { get; init; }
}