using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record DeliveryMethodWriteRequest
{
    [Required, StringLength(120)]
    public required string Name { get; init; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal Price { get; init; }

    [Range(1, 365)]
    public int EstimatedDays { get; init; }
}