using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wishlist")]
public sealed class WishlistController(IWishlistService wishlistService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WishlistItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WishlistItemDto>>> GetWishlist(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return userId is null ? Unauthorized() : Ok(await wishlistService.GetItemsAsync(userId, cancellationToken));
    }

    [HttpPut("{productId:int}")]
    [ProducesResponseType<WishlistItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WishlistItemDto>> AddToWishlist(int productId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var item = await wishlistService.AddItemAsync(userId, productId, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpDelete("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFromWishlist(int productId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        await wishlistService.RemoveItemAsync(userId, productId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{productId:int}/move-to-cart")]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CartDto>> MoveToCart(int productId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        var cart = await wishlistService.MoveToCartAsync(userId, productId, cancellationToken);
        return cart is null ? NotFound() : Ok(cart);
    }

    private string? GetUserId() => User.FindFirstValue(JwtRegisteredClaimNames.Sub);
}