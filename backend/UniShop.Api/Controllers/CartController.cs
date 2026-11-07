using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public sealed class CartController(ICartService cartService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return userId is null ? Unauthorized() : Ok(await cartService.GetCartAsync(userId, cancellationToken));
    }

    [HttpPut("items/{productId:int}")]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartDto>> SetQuantity(
        int productId,
        CartQuantityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var cart = await cartService.SetQuantityAsync(userId, productId, request.Quantity, cancellationToken);
        return cart is null ? NotFound() : Ok(cart);
    }

    [HttpDelete("items/{productId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveItem(int productId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        await cartService.RemoveItemAsync(userId, productId, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        await cartService.ClearCartAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("merge")]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> MergeGuestCart(
        MergeGuestCartRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return Ok(await cartService.MergeGuestCartAsync(userId, request.Items, cancellationToken));
    }

    private string? GetUserId() => User.FindFirstValue(JwtRegisteredClaimNames.Sub);
}