using System.ComponentModel.DataAnnotations;

namespace UniShop.Api.Contracts;

public sealed record UpdateProductStockRequest
{
    [Required]
    [Range(0, int.MaxValue)]
    public int? Stock { get; init; }
}