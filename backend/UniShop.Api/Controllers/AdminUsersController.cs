using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/users")]
public sealed class AdminUsersController(IAdminUserService userService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminUserListDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminUserListDto>>> GetUsers(
        [FromQuery] AdminUserQuery query,
        CancellationToken cancellationToken) =>
        Ok(await userService.GetUsersAsync(query, cancellationToken));

    [HttpPatch("{id}/lock")]
    [ProducesResponseType<AdminUserListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserListDto>> Lock(string id, CancellationToken cancellationToken)
    {
        var currentAdminId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (currentAdminId is null) return Unauthorized();
        var user = await userService.SetLockAsync(id, currentAdminId, true, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPatch("{id}/unlock")]
    [ProducesResponseType<AdminUserListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserListDto>> Unlock(string id, CancellationToken cancellationToken)
    {
        var currentAdminId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (currentAdminId is null) return Unauthorized();
        var user = await userService.SetLockAsync(id, currentAdminId, false, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPatch("{id}/role")]
    [ProducesResponseType<AdminUserListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserListDto>> SetRole(
        string id,
        UpdateAdminUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var currentAdminId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (currentAdminId is null) return Unauthorized();
        var user = await userService.SetRoleAsync(id, currentAdminId, request.Role, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }
}