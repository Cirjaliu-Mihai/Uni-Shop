using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record AddressWriteRequest
{
    [Required, StringLength(80)]
    public required string Label { get; init; }

    [Required, StringLength(200)]
    public required string FullName { get; init; }

    [Required, Phone, StringLength(40)]
    public required string Phone { get; init; }

    [Required, StringLength(300)]
    public required string Street { get; init; }

    [Required, StringLength(120)]
    public required string City { get; init; }

    [Required, StringLength(120)]
    public required string County { get; init; }

    [Required, RegularExpression("^\\d{6}$")]
    public required string PostalCode { get; init; }

    [Required, StringLength(100)]
    public string Country { get; init; } = "Romania";

    public bool IsDefault { get; init; }
}