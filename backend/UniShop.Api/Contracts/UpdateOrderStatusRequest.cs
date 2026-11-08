using System.ComponentModel.DataAnnotations;
using UniShop.Api.Domain;

namespace UniShop.Api.Contracts;

public sealed record UpdateOrderStatusRequest
{
    [EnumDataType(typeof(OrderStatus))]
    public OrderStatus Status { get; init; }
}