using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/checkout")]
public sealed class CheckoutController(IOrderService orderService) : ControllerBase
{
    [HttpGet("delivery-options")]
    [ProducesResponseType<IReadOnlyList<DeliveryOptionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeliveryOptionDto>>> GetDeliveryOptions(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return userId is null ? Unauthorized() : Ok(await orderService.GetDeliveryOptionsAsync(userId, cancellationToken));
    }

    private string? GetUserId() => User.FindFirstValue(JwtRegisteredClaimNames.Sub);
}