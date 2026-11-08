namespace UniShop.Api.Contracts;

public sealed record PopularProductReportDto(int ProductId, string ProductName, int QuantitySold, decimal Revenue);