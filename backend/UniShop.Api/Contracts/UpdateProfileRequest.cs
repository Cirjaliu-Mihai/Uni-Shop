using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record UpdateProfileRequest
{
    [Required, StringLength(200)]
    public required string FullName { get; init; }

    [Phone, StringLength(40)]
    public string? Phone { get; init; }
}