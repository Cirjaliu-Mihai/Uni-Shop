namespace UniShop.Api.Contracts;

public sealed record CategoryListDto(int Id, string Name, string? Description, int ProductCount, string? ImageUrl);