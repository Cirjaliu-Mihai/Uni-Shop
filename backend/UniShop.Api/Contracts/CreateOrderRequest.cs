using System.ComponentModel.DataAnnotations;
using UniShop.Api.Domain;

namespace UniShop.Api.Contracts;

public sealed record CreateOrderRequest
{
    [Range(1, int.MaxValue)]
    public int AddressId { get; init; }

    [Required]
    public required CheckoutContactRequest Contact { get; init; }

    [Range(1, int.MaxValue)]
    public int DeliveryMethodId { get; init; }

    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod PaymentMethod { get; init; }

    [StringLength(1000)]
    public string? Notes { get; init; }
}