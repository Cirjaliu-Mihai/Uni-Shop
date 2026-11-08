namespace UniShop.Api.Contracts;

public sealed record SalesReportPointDto(DateTimeOffset PeriodStart, int OrderCount, decimal Revenue);