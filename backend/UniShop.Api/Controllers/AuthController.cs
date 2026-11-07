using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService) : ControllerBase
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return EmailConflict();
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName.Trim()
        };
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return EmailConflict();
            }

            return BadRequest(new ValidationProblemDetails(createResult.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray())));
        }

        var roleResult = await userManager.AddToRoleAsync(user, "Client");
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new InvalidOperationException("Could not assign the Client role to the new account.");
        }

        var roles = await userManager.GetRolesAsync(user);
        var response = tokenService.CreateToken(user, roles.ToArray());
        return CreatedAtAction(nameof(Me), response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return InvalidCredentials();
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return InvalidCredentials();
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(tokenService.CreateToken(user, roles.ToArray()));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<AuthUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new AuthUserResponse(user.Id, user.Email ?? string.Empty, user.FullName, roles.ToArray()));
    }

    private ConflictObjectResult EmailConflict() => Conflict(new ProblemDetails
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Email already registered",
        Detail = "An account with this email already exists."
    });

    private UnauthorizedObjectResult InvalidCredentials() => Unauthorized(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = InvalidCredentialsMessage,
        Detail = InvalidCredentialsMessage
    });
}