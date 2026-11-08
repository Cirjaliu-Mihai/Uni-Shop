namespace UniShop.Api.Contracts;

public sealed record DeliveryOptionDto(int Id, string Name, decimal Price, int EstimatedDays);