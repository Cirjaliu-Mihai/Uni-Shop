using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    [HttpGet("access-check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AccessCheck() => Ok(new { message = "Admin access granted." });
}