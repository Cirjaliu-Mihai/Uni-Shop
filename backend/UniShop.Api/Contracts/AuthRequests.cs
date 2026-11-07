using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

/// <summary>Details required to register a client account.</summary>
public sealed record RegisterRequest
{
    [Required, StringLength(200)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, MinLength(8), RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,}$")]
    public required string Password { get; init; }
}

/// <summary>Credentials used to authenticate an account.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}