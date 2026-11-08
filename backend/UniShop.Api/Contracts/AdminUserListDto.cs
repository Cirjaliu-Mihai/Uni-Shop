namespace UniShop.Api.Contracts;

public sealed record AdminUserListDto(
    string Id,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    int OrderCount,
    bool IsLocked,
    DateTimeOffset CreatedAt);