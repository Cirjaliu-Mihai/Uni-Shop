using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record ProductSpecificationDto
{
    [Required]
    public required string Key { get; init; }

    [Required]
    public required string Value { get; init; }
}