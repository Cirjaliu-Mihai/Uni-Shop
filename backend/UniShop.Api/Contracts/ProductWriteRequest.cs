using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record ProductWriteRequest
{
    [Required, StringLength(200)]
    public required string Name { get; init; }

    [Required, StringLength(4000)]
    public required string Description { get; init; }

    [SpecificationPairs]
    public List<ProductSpecificationDto> Specifications { get; init; } = [];

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int Stock { get; init; }

    [StringLength(2048), Url]
    public string? ImageUrl { get; init; }

    public bool IsActive { get; init; } = true;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Range(1, int.MaxValue)]
    public int SupplierId { get; init; }

    [Required, MinLength(1)]
    public int[] DeliveryMethodIds { get; init; } = [];
}