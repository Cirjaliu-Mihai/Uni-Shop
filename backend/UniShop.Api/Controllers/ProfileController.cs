using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(ToDto(user));
    }

    [HttpPut]
    [ProducesResponseType<ProfileDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(UpdateProfileRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new ValidationProblemDetails(result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray())));
        }
        return Ok(ToDto(user));
    }

    private static ProfileDto ToDto(ApplicationUser user) => new(
        user.Id,
        user.FullName,
        user.Email ?? string.Empty,
        user.PhoneNumber);
}