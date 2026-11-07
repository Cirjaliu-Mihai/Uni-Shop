using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record CategoryWriteRequest
{
    [Required, StringLength(120)]
    public required string Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }
}