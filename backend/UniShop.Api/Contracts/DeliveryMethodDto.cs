namespace UniShop.Api.Contracts;

public sealed record DeliveryMethodDto(int Id, string Name, decimal Price, int EstimatedDays);