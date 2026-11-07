namespace UniShop.Api.Contracts;

public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, AuthUserResponse User);