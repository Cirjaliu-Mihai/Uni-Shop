using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserListDto>> GetUsersAsync(AdminUserQuery query, CancellationToken cancellationToken);
    Task<AdminUserListDto?> SetLockAsync(string userId, string currentAdminId, bool locked, CancellationToken cancellationToken);
    Task<AdminUserListDto?> SetRoleAsync(string userId, string currentAdminId, string role, CancellationToken cancellationToken);
}