namespace UniShop.Api.Contracts;

public sealed record AuthUserResponse(string Id, string Email, string FullName, IReadOnlyList<string> Roles);