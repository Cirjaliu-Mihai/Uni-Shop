using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class AdminUserService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) : IAdminUserService
{
    public async Task<PagedResult<AdminUserListDto>> GetUsersAsync(AdminUserQuery query, CancellationToken cancellationToken)
    {
        var users = userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = $"%{EscapeLikePattern(query.Search.Trim())}%";
            users = users.Where(user => EF.Functions.ILike(user.Email!, search, "\\") || EF.Functions.ILike(user.FullName, search, "\\"));
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var page = Math.Max(query.Page ?? 1, 1);
        var pageSize = Math.Clamp(query.PageSize ?? 20, 1, 50);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var pageUsers = await users
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.FullName,
                user.CreatedAt,
                user.LockoutEnd,
                OrderCount = user.Orders.Count()
            })
            .ToListAsync(cancellationToken);

        var items = new List<AdminUserListDto>(pageUsers.Count);
        foreach (var user in pageUsers)
        {
            var roles = await userManager.GetRolesAsync(new ApplicationUser { Id = user.Id });
            items.Add(new AdminUserListDto(
                user.Id,
                user.Email ?? string.Empty,
                user.FullName,
                roles.Order(StringComparer.Ordinal).ToArray(),
                user.OrderCount,
                user.LockoutEnd > DateTimeOffset.UtcNow,
                user.CreatedAt));
        }

        return new PagedResult<AdminUserListDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<AdminUserListDto?> SetLockAsync(
        string userId,
        string currentAdminId,
        bool locked,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return null;
        if (locked && user.Id == currentAdminId)
        {
            throw new AdminConflictException("Administrators cannot lock their own account.");
        }

        if (locked && await userManager.IsInRoleAsync(user, "Admin") &&
            await userManager.GetUsersInRoleAsync("Admin") is { Count: <= 1 })
        {
            throw new AdminConflictException("The last administrator cannot be locked.");
        }

        var result = await userManager.SetLockoutEndDateAsync(
            user,
            locked ? DateTimeOffset.UtcNow.AddYears(100) : null);
        EnsureSucceeded(result, "Could not update the user's lock state.");
        if (!locked)
        {
            EnsureSucceeded(await userManager.ResetAccessFailedCountAsync(user), "Could not reset the user's failed access count.");
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetUserAsync(user);
    }

    public async Task<AdminUserListDto?> SetRoleAsync(
        string userId,
        string currentAdminId,
        string role,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (role is not ("Admin" or "Client"))
        {
            throw new AdminConflictException("Role must be Admin or Client.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return null;
        var roles = await userManager.GetRolesAsync(user);
        if (role == "Client" && roles.Contains("Admin", StringComparer.Ordinal))
        {
            if (user.Id == currentAdminId)
            {
                throw new AdminConflictException("Administrators cannot demote themselves.");
            }
            if (await userManager.GetUsersInRoleAsync("Admin") is { Count: <= 1 })
            {
                throw new AdminConflictException("The last administrator cannot be demoted.");
            }
        }

        foreach (var existingRole in roles.Where(existingRole => existingRole != role))
        {
            EnsureSucceeded(await userManager.RemoveFromRoleAsync(user, existingRole), "Could not remove the user's existing role.");
        }
        if (!roles.Contains(role, StringComparer.Ordinal))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, role), "Could not assign the requested role.");
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetUserAsync(user);
    }

    private async Task<AdminUserListDto> GetUserAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var orderCount = await dbContext.Orders.CountAsync(order => order.UserId == user.Id);
        return new AdminUserListDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            roles.Order(StringComparer.Ordinal).ToArray(),
            orderCount,
            await userManager.IsLockedOutAsync(user),
            user.CreatedAt);
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new AdminConflictException($"{message} {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }

    private static string EscapeLikePattern(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}