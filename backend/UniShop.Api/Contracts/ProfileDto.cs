namespace UniShop.Api.Contracts;

public sealed record ProfileDto(string Id, string FullName, string Email, string? Phone);