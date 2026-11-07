namespace UniShop.Api.Services;

/// <summary>Authenticated user details returned by the API.</summary>
public sealed record AuthUserResponse(string Id, string Email, string FullName, IReadOnlyList<string> Roles);

/// <summary>Bearer token and authenticated user details.</summary>
public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, AuthUserResponse User);