namespace UniShop.Api.Contracts;

public sealed record CategoryAdminDto(int Id, string Name, string? Description, int ProductCount);