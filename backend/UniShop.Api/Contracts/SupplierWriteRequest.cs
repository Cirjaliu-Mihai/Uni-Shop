using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record SupplierWriteRequest
{
    [Required, StringLength(160)]
    public required string Name { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string ContactEmail { get; init; }

    [Required, StringLength(40)]
    public required string Phone { get; init; }
}